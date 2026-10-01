// Network for code project candidate machines. Deploy once per region into its own resource group:
//
//   az deployment group create -g rg-rb-codeprojects -f infra/code-projects/main.bicep \
//     -p packageMirrorPrefixes='["203.0.113.10/32"]'
//
// Then set, in Risebird.AzureTimerFunctions configuration:
//   CodeProjects:Azure:ResourceGroup = rg-rb-codeprojects
//   CodeProjects:Azure:Location      = <location>
//   CodeProjects:Azure:SubnetId      = output candidateSubnetId
//   CodeProjects:Azure:BastionName   = output bastionName
//
// What this enforces (Code Project Question Spec, §09 Security):
//   - Dedicated VNet with no peering to production (none is declared here, and none should be added).
//   - No public IP on candidate machines; the only way in is Azure Bastion (3389 never internet-facing).
//   - Bastion Standard with shareable links, so a candidate connects from a browser without an Azure account.
//   - Egress allowlist: Azure Storage (project zip, Run Command output) and the package mirrors only.
// IMDS/WireServer cannot be filtered by an NSG; the machine setup script blocks them for the
// candidate account with an in-guest firewall rule instead.

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Name prefix.')
param prefix string = 'rb-cp'

@description('Address space of the dedicated candidate VNet. Must not overlap anything it could ever be peered with.')
param vnetAddressPrefix string = '10.240.0.0/16'

@description('Candidate machine subnet.')
param candidateSubnetPrefix string = '10.240.0.0/20'

@description('AzureBastionSubnet, /26 or larger.')
param bastionSubnetPrefix string = '10.240.255.0/26'

@description('CIDR prefixes of the package mirrors candidates may reach (Maven/npm/PyPI/NuGet proxies). Empty means none.')
param packageMirrorPrefixes array = []

resource candidateNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${prefix}-candidate-nsg'
  location: location
  properties: {
    securityRules: concat([
      {
        name: 'Allow-RDP-From-Bastion'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourceAddressPrefix: bastionSubnetPrefix
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '3389'
        }
      }
      {
        name: 'Deny-All-Other-Inbound'
        properties: {
          priority: 4000
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '*'
        }
      }
      {
        // Project zip download and Run Command output blobs.
        name: 'Allow-Storage-Out'
        properties: {
          priority: 100
          direction: 'Outbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: 'Storage'
          destinationPortRange: '443'
        }
      }
    ], empty(packageMirrorPrefixes) ? [] : [
      {
        name: 'Allow-Package-Mirrors-Out'
        properties: {
          priority: 200
          direction: 'Outbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefixes: packageMirrorPrefixes
          destinationPortRanges: [ '80', '443' ]
        }
      }
    ], [
      {
        name: 'Deny-Internet-Out'
        properties: {
          priority: 4000
          direction: 'Outbound'
          access: 'Deny'
          protocol: '*'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: 'Internet'
          destinationPortRange: '*'
        }
      }
      {
        // Candidates cannot reach one another.
        name: 'Deny-VNet-Out'
        properties: {
          priority: 4001
          direction: 'Outbound'
          access: 'Deny'
          protocol: '*'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: 'VirtualNetwork'
          destinationPortRange: '*'
        }
      }
    ])
  }
}

resource natIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: '${prefix}-nat-pip'
  location: location
  sku: { name: 'Standard' }
  properties: { publicIPAllocationMethod: 'Static' }
}

// Explicit outbound path for the allowlisted destinations (default outbound access is off).
resource nat 'Microsoft.Network/natGateways@2024-05-01' = {
  name: '${prefix}-nat'
  location: location
  sku: { name: 'Standard' }
  properties: { publicIpAddresses: [ { id: natIp.id } ] }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: '${prefix}-vnet'
  location: location
  properties: {
    addressSpace: { addressPrefixes: [ vnetAddressPrefix ] }
    subnets: [
      {
        name: 'candidates'
        properties: {
          addressPrefix: candidateSubnetPrefix
          networkSecurityGroup: { id: candidateNsg.id }
          natGateway: { id: nat.id }
          // No default outbound access: egress only through the NAT gateway, as the NSG allows.
          defaultOutboundAccess: false
        }
      }
      {
        name: 'AzureBastionSubnet'
        properties: { addressPrefix: bastionSubnetPrefix }
      }
    ]
  }
}

resource bastionIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: '${prefix}-bastion-pip'
  location: location
  sku: { name: 'Standard' }
  properties: { publicIPAllocationMethod: 'Static' }
}

resource bastion 'Microsoft.Network/bastionHosts@2024-05-01' = {
  name: '${prefix}-bastion'
  location: location
  sku: { name: 'Standard' }   // shareable links need Standard or higher
  properties: {
    enableShareableLink: true
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          subnet: { id: '${vnet.id}/subnets/AzureBastionSubnet' }
          publicIPAddress: { id: bastionIp.id }
        }
      }
    ]
  }
}

output candidateSubnetId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnet.name, 'candidates')
output bastionName string = bastion.name
output natOutboundIp string = natIp.properties.ipAddress
