# Code project candidate machines — Azure setup

> **Use the one-click setup instead of these manual steps.** Admin dashboard → "Code project
> infrastructure" → **Set up**. It calls `POST /api/codeprojects/setup/start` on the functions app,
> which does everything below through ARM: database migrations, network, Bastion, VM images
> (Azure Image Builder), grading runners (ACR + Container Apps jobs), `StackImages` rows and the settings
> both apps read (`CodeProjectSettings` table). Code: `Risebird.AzureTimerFunctions/CodeProjects/Setup`.
> The notes below remain as a reference for what it builds; `main.bicep` is an equivalent of the network part.

## Cost and teardown

Everything billable is created in **one resource group, `rg-rb-codeprojects`** (tag `purpose=rb-code-projects`).
Deleting that group removes the feature's whole Azure footprint. Azure also creates two groups it manages
itself; both disappear when their parent resource goes with `rg-rb-codeprojects`:

| Group | Created by | Removed when |
| --- | --- | --- |
| `IT_rg-rb-codeprojects_rbcp-…` (one per image template) | Azure Image Builder | its image template is deleted |
| `ME_rb-cp-runners_rg-rb-codeprojects_<region>` | Container Apps environment | the environment is deleted |

Outside Azure resource groups, the feature also leaves (free or near-free) blob containers `codeprojects`,
`codeprojectsecrets`, `codeprojectcollect`, `codeprojectruns`, `codeprojectbuild` in the app's existing
storage account, and its tables in SQL. After deleting the group, set `StackImages.IsActive = 0` so authors
can't generate questions for machines that no longer exist; running setup again recreates everything.

Approximate always-on cost (pay-as-you-go list prices, check your region in the Azure pricing calculator):

| Resource | Why it is needed | ≈ per month |
| --- | --- | --- |
| Bastion Standard | browser RDP via shareable links (Basic/Developer have no shareable links) | ~$210 |
| NAT gateway + its public IP | explicit egress (default outbound access is retired) | ~$35 |
| Bastion public IP | required by Bastion | ~$4 |
| Container registry Basic | runner images | ~$5 |
| Gallery image versions | one image per enabled stack | ~$2–3 per stack |
| Container Apps (Consumption profile) | runner jobs; pay per second while a job runs | ~$0 idle |

Per interview: one Windows D2s_v5 for ~25 minutes plus one or two short runner executions, roughly $0.10.

Cost settings: `CodeProjects:Setup:Stacks` (build only the stacks you use), `CodeProjects:Setup:MonthlyBudget` +
`BudgetEmails` (Cost Management alert at 80% actual / 100% forecast), `CodeProjects:Azure:LicenseType = Windows_Server`
(Azure Hybrid Benefit, only with eligible licences), `CodeProjects:Azure:MaxLifetimeMinutes` (default 75).

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
