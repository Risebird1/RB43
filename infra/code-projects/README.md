# Code project candidate machines — Azure setup

The provisioner lives in `Risebird.AzureTimerFunctions/CodeProjects`. The web app calls it at
`CodeProjects:ProvisionerUrl`. Setup order:

## 1. Network and Bastion

```
az deployment group create -g rg-rb-codeprojects -f infra/code-projects/main.bicep \
  -p packageMirrorPrefixes='["<mirror CIDR>"]'
```

This creates a dedicated VNet (never peer it to production), a candidate subnet whose NSG allows
RDP only from Bastion and egress only to Azure Storage and the package mirrors, a NAT gateway
for that egress, and Bastion **Standard** with shareable links enabled.

## 2. Permissions for the functions app's service principal (`AzureVmCredentials:*`)

| Scope | Role | Why |
| --- | --- | --- |
| `rg-rb-codeprojects` | Virtual Machine Contributor | VMs and their Run Commands |
| `rg-rb-codeprojects` | Network Contributor | NICs, subnet join, Bastion shareable links |
| The Compute Gallery | Reader | Create VMs from its image versions |

## 3. Stack images (the spec's "real first task")

Bake one Windows image per stack into an Azure Compute Gallery. Each image needs:

- **Generalized (sysprepped).** If it is specialized, set `CodeProjects:Azure:ImageIsSpecialized = true`.
- **A security type matching `CodeProjects:Azure:SecurityType`.** The default is `TrustedLaunch`.
- **The Azure VM agent.** Gallery images built from marketplace Windows have it.
- **`git` on the machine PATH.** The setup script runs as LocalSystem and the candidate runs as a new standard user, so git must be visible to both.
- **The toolchain and IDE**, for example JDK 21, Maven and IntelliJ for `rb-java-win`.
- **Pre-seeded dependencies that a brand-new user can see.** The candidate account is created at handover, so a cache in the baking user's profile is invisible to it. Use machine-wide locations, for example a Maven `settings.xml` under the Maven install pointing `localRepository` at `C:\m2`, or npm/NuGet caches outside `C:\Users\<name>`. The generated commands use offline flags (`mvn -o`).

Register each image in `StackImages`. Put the image definition id in `GalleryImageId`: the id without `/versions/...` resolves to the latest version, and a full version id pins it.

## 4. Configuration

**Functions app** (`Risebird.AzureTimerFunctions`):
- `CodeProjects:Azure:ResourceGroup`, `Location`, `SubnetId` and `BastionName` come from the Bicep outputs.
- `CodeProjects:StorageConnectionString` must point at the same storage account the web app uses.

**Web app:**
- `CodeProjects:ProvisionerUrl` = `https://<functions-app>.azurewebsites.net/api/codeprojects`.
- `CodeProjects:ProvisionerKey` = a function key for that app.

## What happens per candidate

1. The VM is created from the gallery image, with no public IP, no managed identity, and automatic updates off.
2. A Run Command (`rb-setup`) runs as LocalSystem. It creates the standard user `candidate` (never an administrator) and blocks IMDS and WireServer for that user with an in-guest firewall rule; an NSG can't filter those endpoints. It then pulls `project.zip` into `C:\workspace`, commits a baseline, and stores the baseline SHA where the candidate can't change it.
3. Bastion creates a shareable link. The web app shows the link together with the `candidate` sign-in, because the link carries no credentials.
4. On submit, a second Run Command (`rb-collect`) reads the last commit. Its output goes to a private append blob. The VM is then deleted, and its NIC and OS disk go with it.
5. `CodeProjectVmJanitor` deletes failed machines, and any older than `CodeProjects:Azure:MaxLifetimeMinutes`.

Hidden tests and the reference solution never reach the VM. They stay in the web app's secrets container and only run in the grading container.
