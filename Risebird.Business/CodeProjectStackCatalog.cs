using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Risebird.Business
{
    /// <summary>
    /// One stack = one StackImages row, one Windows VM image (candidate machine) and one Linux runner
    /// container (validation and grading). Both are built from the same pinned dependency list, so a
    /// project that builds offline in the runner also builds offline on the candidate's machine.
    /// The toolchain text is what the generator sees in its allowlist, including the only
    /// dependencies it may use and the exact build/test commands.
    /// </summary>
    public sealed class CodeProjectStack
    {
        public string Code { get; init; }
        public string DisplayName { get; init; }
        /// <summary>At most 500 characters (StackImages.Toolchain).</summary>
        public string Toolchain { get; init; }
        /// <summary>Container Apps job name: lowercase, at most 32 characters.</summary>
        public string JobName { get; init; }
        public string RunnerRepository { get; init; }
        public string RunnerBaseImage { get; init; }
        public string RunnerSeedDockerfile { get; init; }
        public IReadOnlyDictionary<string, string> SeedFiles { get; init; }
        public string WindowsInstallScript { get; init; }
        public string WindowsSeedScript { get; init; }
    }

    public static class CodeProjectStackCatalog
    {
        public const string WindowsSourcePublisher = "MicrosoftWindowsServer";
        public const string WindowsSourceOffer = "WindowsServer";
        public const string WindowsSourceSku = "2022-datacenter-azure-edition";

        // ------------------------------------------------------------------ shared pieces

        /// <summary>
        /// Container entrypoint. Downloads the project, runs build then test as an unprivileged user,
        /// and uploads exit codes, logs and every JUnit/TRX report as a tar.gz. The SAS urls are
        /// removed from the environment before any project code runs, so the code under test can
        /// neither re-read the input nor overwrite the result.
        /// </summary>
        public const string RunnerScript = """
#!/usr/bin/env bash
# Risebird code project runner. Env: ZIP_URL, RESULT_URL, BUILD_CMD, TEST_CMD, RB_TIMEOUT
set -u
ZIP="${ZIP_URL:-}"; RESULT="${RESULT_URL:-}"
unset ZIP_URL RESULT_URL
OUT=/tmp/rb-out; WS=/home/runner/workspace
rm -rf "$OUT" "$WS"; mkdir -p "$OUT/reports" "$WS"

upload() {
  tar -czf /tmp/rb-out.tgz -C "$OUT" . 2>/dev/null
  curl -sS --retry 3 -X PUT -H "x-ms-blob-type: BlockBlob" -H "Content-Type: application/gzip" \
       --data-binary @/tmp/rb-out.tgz "$RESULT" > /dev/null
}

if ! curl -sS --retry 3 -f -o /tmp/rb-in.zip "$ZIP"; then echo "Could not download the project." > "$OUT/error.txt"; upload; exit 0; fi
if ! unzip -q /tmp/rb-in.zip -d "$WS"; then echo "Could not unpack the project." > "$OUT/error.txt"; upload; exit 0; fi
rm -f /tmp/rb-in.zip
chown -R runner:runner "$WS"
cd "$WS"

T="${RB_TIMEOUT:-120}"
start=$(date +%s%N)
timeout -k 5 "$T" runuser -u runner -- bash -c "cd '$WS' && ${BUILD_CMD:-true}" > "$OUT/build.log" 2>&1
echo $? > "$OUT/build.exit"
if [ "$(cat "$OUT/build.exit")" = "0" ]; then
  timeout -k 5 "$T" runuser -u runner -- bash -c "cd '$WS' && ${TEST_CMD:-true}" > "$OUT/test.log" 2>&1
  echo $? > "$OUT/test.exit"
fi
end=$(date +%s%N)
echo $(( (end - start) / 1000000 )) > "$OUT/wall.ms"

n=0
while IFS= read -r -d '' f; do
  n=$((n+1)); cp "$f" "$OUT/reports/$n-$(basename "$f")"
done < <(find "$WS" -type f \( -path '*/surefire-reports/*.xml' -o -path '*/test-results/*.xml' -o -name '*.trx' \) -print0 2>/dev/null)

for f in build.log test.log; do
  if [ -f "$OUT/$f" ]; then tail -c 20000 "$OUT/$f" > "$OUT/$f.tmp" && mv "$OUT/$f.tmp" "$OUT/$f"; fi
done
upload
exit 0
""";

        private const string DockerfileHeader = """
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl unzip ca-certificates && rm -rf /var/lib/apt/lists/*
RUN id -u runner >/dev/null 2>&1 || useradd -m -u 10001 runner
COPY seed /opt/rb-seed
""";

        private const string DockerfileFooter = """
COPY rb-run.sh /usr/local/bin/rb-run
RUN sed -i 's/\r$//' /usr/local/bin/rb-run && chmod 0755 /usr/local/bin/rb-run
ENTRYPOINT ["/usr/local/bin/rb-run"]
""";

        /// <summary>Chocolatey, git and VS Code on every candidate machine. Runs as LocalSystem, elevated.</summary>
        private const string WindowsCommonInstall = """
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor 3072
Set-ExecutionPolicy Bypass -Scope Process -Force
function Wait-Retry([int]$attempt) { Start-Sleep -Seconds ([Math]::Min(300, 20 * [Math]::Pow(2, $attempt))) }
# The community feed rate-limits and has outages (503s), so everything that touches it retries with backoff.
for ($attempt = 1; -not (Get-Command choco -ErrorAction SilentlyContinue); $attempt++) {
    try { Invoke-Expression ((New-Object Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1')) }
    catch { if ($attempt -ge 6) { throw } ; Write-Host "Chocolatey install attempt $attempt failed: $_" ; Wait-Retry $attempt }
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine')
}
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine')
# choco can exit 0 when the feed fails and nothing was installed, so success means "now listed as installed".
function Invoke-Choco([string[]]$packageArgs) {
    $id = $packageArgs[0]
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        & choco install @packageArgs -y --no-progress
        $code = $LASTEXITCODE
        $installed = @(& choco list --limit-output --exact $id) -match ('^' + [regex]::Escape($id) + '\|')
        if (($code -eq 0 -or $code -eq 3010) -and $installed) { return }
        Write-Host "choco install $id attempt $attempt failed (exit $code, installed: $([bool]$installed))"
        if ($attempt -lt 6) { Wait-Retry $attempt }
    }
    throw "choco install $($packageArgs -join ' ') failed after 6 attempts"
}
Invoke-Choco @('git')
Invoke-Choco @('vscode')
""";

        /// <summary>Runs after a restart, so every installed tool is on the machine PATH.</summary>
        private const string WindowsCommonSeedHeader = """
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine')
function Invoke-Native([string]$command) {
    cmd /c $command
    if ($LASTEXITCODE -ne 0) { throw "failed ($LASTEXITCODE): $command" }
}
function Grant-Users([string]$path) {
    icacls $path /grant '*S-1-5-32-545:(OI)(CI)M' /T /Q | Out-Null   # BUILTIN\Users: the candidate is a brand-new local user
}
""";

        // ------------------------------------------------------------------ seed projects

        private const string JavaPom = """
<?xml version="1.0" encoding="UTF-8"?>
<project xmlns="http://maven.apache.org/POM/4.0.0" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 https://maven.apache.org/xsd/maven-4.0.0.xsd">
  <modelVersion>4.0.0</modelVersion>
  <parent>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-parent</artifactId>
    <version>3.3.5</version>
    <relativePath/>
  </parent>
  <groupId>io.risebird</groupId>
  <artifactId>seed</artifactId>
  <version>1.0.0</version>
  <properties><java.version>21</java.version></properties>
  <dependencies>
    <dependency><groupId>org.springframework.boot</groupId><artifactId>spring-boot-starter-web</artifactId></dependency>
    <dependency><groupId>org.springframework.boot</groupId><artifactId>spring-boot-starter-validation</artifactId></dependency>
    <dependency><groupId>org.springframework.boot</groupId><artifactId>spring-boot-starter-data-jpa</artifactId></dependency>
    <dependency><groupId>com.h2database</groupId><artifactId>h2</artifactId><scope>runtime</scope></dependency>
    <dependency><groupId>org.springframework.boot</groupId><artifactId>spring-boot-starter-test</artifactId><scope>test</scope></dependency>
  </dependencies>
  <build>
    <plugins>
      <plugin><groupId>org.springframework.boot</groupId><artifactId>spring-boot-maven-plugin</artifactId></plugin>
    </plugins>
  </build>
</project>
""";

        private const string JavaApp = """
package io.risebird.seed;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class SeedApplication {
    public static void main(String[] args) { SpringApplication.run(SeedApplication.class, args); }
}
""";

        private const string JavaTest = """
package io.risebird.seed;

import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;

@SpringBootTest
class SeedApplicationTests {
    @Test
    void contextLoads() { }
}
""";

        private const string NodePackage = """
{
  "name": "rb-seed",
  "version": "1.0.0",
  "private": true,
  "type": "module",
  "dependencies": {
    "express": "4.21.1",
    "react": "18.3.1",
    "react-dom": "18.3.1"
  },
  "devDependencies": {
    "@testing-library/react": "16.0.1",
    "@types/express": "5.0.0",
    "@types/node": "22.9.0",
    "@vitejs/plugin-react": "4.3.3",
    "jsdom": "25.0.1",
    "supertest": "7.0.0",
    "typescript": "5.6.3",
    "vite": "5.4.10",
    "vitest": "2.1.4"
  }
}
""";

        private const string PythonRequirements = """
Django==5.1.3
djangorestframework==3.15.2
Flask==3.0.3
requests==2.32.3
pytest==8.3.3
pytest-django==4.9.0
""";

        private const string DotnetApiProject = """
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
""";

        private const string DotnetApiProgram = """
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/health", () => "ok");
app.Run();
public partial class Program { }
""";

        private const string DotnetTestProject = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.10" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../Seed.Api/Seed.Api.csproj" />
  </ItemGroup>
</Project>
""";

        private const string DotnetTest = """
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public HealthTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_is_ok() => Assert.Equal("ok", await _factory.CreateClient().GetStringAsync("/health"));
}
""";

        // ------------------------------------------------------------------ stacks

        public static readonly IReadOnlyList<CodeProjectStack> All = new[]
        {
            new CodeProjectStack
            {
                Code = "rb-java-win",
                DisplayName = "Java / Spring Boot",
                Toolchain = "Windows, JDK 21 (Temurin), Maven 3.9, IntelliJ IDEA CE. Offline Maven deps ONLY: spring-boot-starter-parent 3.3.5 with starter-web, starter-validation, starter-data-jpa, h2, starter-test (JUnit 5, Mockito). build_command: mvn -q -o compile. test_command: mvn -q -o test (Surefire XML reports).",
                JobName = "rbcp-run-java",
                RunnerRepository = "rbcp-runner-java",
                RunnerBaseImage = "maven:3.9-eclipse-temurin-21",
                SeedFiles = new Dictionary<string, string>
                {
                    ["java/pom.xml"] = JavaPom,
                    ["java/src/main/java/io/risebird/seed/SeedApplication.java"] = JavaApp,
                    ["java/src/test/java/io/risebird/seed/SeedApplicationTests.java"] = JavaTest
                },
                RunnerSeedDockerfile = """
ENV MAVEN_OPTS=-Dmaven.repo.local=/opt/m2
RUN mkdir -p /opt/m2 && cd /opt/rb-seed/java \
 && mvn -B -q -Dmaven.repo.local=/opt/m2 package \
 && mvn -B -q -Dmaven.repo.local=/opt/m2 dependency:go-offline \
 && rm -rf /opt/rb-seed/java/target && chown -R runner:runner /opt/m2
""",
                WindowsInstallScript = "Invoke-Choco @('temurin21')\nInvoke-Choco @('maven')\nInvoke-Choco @('intellijidea-community')\n",
                WindowsSeedScript = """
New-Item -ItemType Directory -Path 'C:\m2' -Force | Out-Null
Push-Location 'C:\rb-seed\java'
Invoke-Native 'mvn -B -q -Dmaven.repo.local=C:\m2 package'
Invoke-Native 'mvn -B -q -Dmaven.repo.local=C:\m2 dependency:go-offline'
Pop-Location
[Environment]::SetEnvironmentVariable('MAVEN_OPTS', '-Dmaven.repo.local=C:\m2', 'Machine')
Grant-Users 'C:\m2'
"""
            },
            new CodeProjectStack
            {
                Code = "rb-node-win",
                DisplayName = "Node / React",
                Toolchain = "Windows, Node 20, npm 10, VS Code. Offline npm deps ONLY, exact versions: express 4.21.1, react 18.3.1, react-dom 18.3.1, vitest 2.1.4, supertest 7.0.0, typescript 5.6.3, vite 5.4.10, @vitejs/plugin-react 4.3.3, jsdom 25.0.1, @testing-library/react 16.0.1, @types/node 22.9.0, @types/express 5.0.0. build_command: npm install --offline --no-audit --no-fund. test_command: npx vitest run --reporter=junit --outputFile=test-results/junit.xml",
                JobName = "rbcp-run-node",
                RunnerRepository = "rbcp-runner-node",
                RunnerBaseImage = "node:20-bookworm",
                SeedFiles = new Dictionary<string, string> { ["node/package.json"] = NodePackage },
                RunnerSeedDockerfile = """
ENV npm_config_cache=/opt/npm-cache npm_config_update_notifier=false
RUN mkdir -p /opt/npm-cache && cd /opt/rb-seed/node && npm install --no-audit --no-fund \
 && rm -rf /opt/rb-seed/node/node_modules && chown -R runner:runner /opt/npm-cache
""",
                WindowsInstallScript = "Invoke-Choco @('nodejs', '--version=20.18.0')\n",
                WindowsSeedScript = """
New-Item -ItemType Directory -Path 'C:\npm-cache' -Force | Out-Null
[Environment]::SetEnvironmentVariable('npm_config_cache', 'C:\npm-cache', 'Machine')
$env:npm_config_cache = 'C:\npm-cache'
Push-Location 'C:\rb-seed\node'
Invoke-Native 'npm install --no-audit --no-fund'
Pop-Location
Grant-Users 'C:\npm-cache'
"""
            },
            new CodeProjectStack
            {
                Code = "rb-python-win",
                DisplayName = "Python / Django",
                Toolchain = "Windows, Python 3.12, VS Code. Installed packages ONLY (no install step): Django 5.1.3, djangorestframework 3.15.2, Flask 3.0.3, requests 2.32.3, pytest 8.3.3, pytest-django 4.9.0. build_command: python -m compileall -q . test_command: python -m pytest --junitxml=test-results/junit.xml",
                JobName = "rbcp-run-python",
                RunnerRepository = "rbcp-runner-python",
                RunnerBaseImage = "python:3.12-bookworm",
                SeedFiles = new Dictionary<string, string> { ["python/requirements.txt"] = PythonRequirements },
                RunnerSeedDockerfile = """
RUN pip install --no-cache-dir -r /opt/rb-seed/python/requirements.txt
""",
                WindowsInstallScript = "Invoke-Choco @('python312')\n",
                WindowsSeedScript = """
Invoke-Native 'python -m pip install --no-warn-script-location -r C:\rb-seed\python\requirements.txt'
"""
            },
            new CodeProjectStack
            {
                Code = "rb-dotnet-win",
                DisplayName = ".NET 8 / ASP.NET Core",
                Toolchain = "Windows, .NET 8 SDK, VS Code. Offline NuGet ONLY: Microsoft.NET.Test.Sdk 17.11.1, xunit 2.9.2, xunit.runner.visualstudio 2.8.2, Microsoft.AspNetCore.Mvc.Testing 8.0.10 (plus the shared framework). build_command: dotnet build -nologo. test_command: dotnet test -nologo --logger trx --results-directory TestResults",
                JobName = "rbcp-run-dotnet",
                RunnerRepository = "rbcp-runner-dotnet",
                RunnerBaseImage = "mcr.microsoft.com/dotnet/sdk:8.0",
                SeedFiles = new Dictionary<string, string>
                {
                    ["dotnet/Seed.Api/Seed.Api.csproj"] = DotnetApiProject,
                    ["dotnet/Seed.Api/Program.cs"] = DotnetApiProgram,
                    ["dotnet/Seed.Tests/Seed.Tests.csproj"] = DotnetTestProject,
                    ["dotnet/Seed.Tests/HealthTests.cs"] = DotnetTest
                },
                RunnerSeedDockerfile = """
ENV NUGET_PACKAGES=/opt/nuget DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
RUN cd /opt/rb-seed/dotnet/Seed.Tests && dotnet test -nologo --logger trx --results-directory /tmp/seed-results \
 && rm -rf /tmp/seed-results /opt/rb-seed/dotnet/*/bin /opt/rb-seed/dotnet/*/obj && chown -R runner:runner /opt/nuget
""",
                WindowsInstallScript = "Invoke-Choco @('dotnet-8.0-sdk')\n",
                WindowsSeedScript = """
New-Item -ItemType Directory -Path 'C:\nuget' -Force | Out-Null
[Environment]::SetEnvironmentVariable('NUGET_PACKAGES', 'C:\nuget', 'Machine')
[Environment]::SetEnvironmentVariable('DOTNET_CLI_TELEMETRY_OPTOUT', '1', 'Machine')
$env:NUGET_PACKAGES = 'C:\nuget'
Push-Location 'C:\rb-seed\dotnet\Seed.Tests'
Invoke-Native 'dotnet test -nologo'
Pop-Location
Grant-Users 'C:\nuget'
"""
            }
        };

        public static CodeProjectStack Find(string code) =>
            All.FirstOrDefault(s => string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));

        // ------------------------------------------------------------------ runner container

        public static string RunnerDockerfile(CodeProjectStack stack) =>
            $"FROM {stack.RunnerBaseImage}\n{DockerfileHeader}\n{stack.RunnerSeedDockerfile}\n{DockerfileFooter}\n";

        /// <summary>Content hash: a change to the Dockerfile, the runner script or the seed produces a new image tag.</summary>
        public static string RunnerImageTag(CodeProjectStack stack) =>
            Hash(RunnerDockerfile(stack) + RunnerScript + string.Concat(stack.SeedFiles.OrderBy(f => f.Key).Select(f => f.Key + f.Value)));

        /// <summary>The docker build context for ACR: Dockerfile, rb-run.sh and seed/..., as tar.gz.</summary>
        public static byte[] BuildRunnerContext(CodeProjectStack stack)
        {
            var files = new Dictionary<string, string>
            {
                ["Dockerfile"] = RunnerDockerfile(stack),
                ["rb-run.sh"] = RunnerScript
            };
            foreach (var f in stack.SeedFiles) files["seed/" + f.Key] = f.Value;

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            using (var tar = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: true))
            {
                foreach (var f in files.OrderBy(f => f.Key, StringComparer.Ordinal))
                {
                    var bytes = new UTF8Encoding(false).GetBytes(f.Value.Replace("\r\n", "\n"));
                    var entry = new PaxTarEntry(TarEntryType.RegularFile, f.Key)
                    {
                        DataStream = new MemoryStream(bytes),
                        Mode = f.Key.EndsWith(".sh", StringComparison.Ordinal)
                            ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute
                            : UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead
                    };
                    tar.WriteEntry(entry);
                }
            }
            return output.ToArray();
        }

        // ------------------------------------------------------------------ Windows image (Azure Image Builder)

        /// <summary>First customizer: tools. A restart follows so the machine PATH is complete.</summary>
        public static string[] WindowsInstallLines(CodeProjectStack stack) =>
            // Raw string literals do not end with a newline, so blocks are joined with one explicitly.
            Lines(WindowsCommonInstall + "\n" + stack.WindowsInstallScript);

        /// <summary>Second customizer: writes the seed project and pre-downloads its dependencies machine-wide.</summary>
        public static string[] WindowsSeedLines(CodeProjectStack stack)
        {
            var sb = new StringBuilder(WindowsCommonSeedHeader).Append('\n');
            foreach (var f in stack.SeedFiles.OrderBy(f => f.Key, StringComparer.Ordinal))
            {
                var path = @"C:\rb-seed\" + f.Key.Replace('/', '\\');
                var b64 = Convert.ToBase64String(new UTF8Encoding(false).GetBytes(f.Value.Replace("\r\n", "\n")));
                // Base64 keeps quotes, braces and $ in the seed files out of PowerShell's way.
                sb.Append($"New-Item -ItemType Directory -Path '{Path.GetDirectoryName(path)}' -Force | Out-Null\n");
                sb.Append($"[IO.File]::WriteAllBytes('{path}', [Convert]::FromBase64String('{b64}'))\n");
            }
            sb.Append(stack.WindowsSeedScript).Append('\n');
            sb.Append("Remove-Item 'C:\\rb-seed' -Recurse -Force -ErrorAction SilentlyContinue\n");
            return Lines(sb.ToString());
        }

        /// <summary>Image Builder templates cannot be changed after creation, so the name carries a content hash.</summary>
        public static string ImageTemplateName(CodeProjectStack stack) =>
            $"rbcp-{stack.Code}-{Hash(string.Join("\n", WindowsInstallLines(stack)) + string.Join("\n", WindowsSeedLines(stack)) + WindowsSourceSku + ImageRecipeVersion)}";

        /// <summary>Bump when the template's fixed customizers change (e.g. the Windows Update step), so a new template is created.</summary>
        public const string ImageRecipeVersion = "3-line-breaks-cleanup-on-error";

        private static string[] Lines(string script) =>
            script.Replace("\r\n", "\n").Split('\n').Where(l => l.Length > 0).ToArray();

        public static string Hash(string content)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content.Replace("\r\n", "\n")));
            return Convert.ToHexString(bytes).Substring(0, 10).ToLowerInvariant();
        }
    }
}
