#!/usr/bin/env bash
# Runs the Risebird unit-test suites with coverage and prints a per-project summary.
# Usage: ./coverage.sh [--html]
set -uo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "Cleaning previous coverage output..."
find "$root" -type d -name TestResults -prune -exec rm -rf {} + 2>/dev/null || true

# The gate: Risebird.Business + Risebird.CommonHelper.
for project in \
    Risebird.CommonHelper.Tests/Risebird.CommonHelper.Tests.csproj \
    Risebird.Business.Tests/Risebird.Business.Tests.csproj
do
    echo
    echo "Running $project (gated)"
    dotnet test "$root/$project" \
        --settings "$root/coverage.runsettings" \
        --logger 'console;verbosity=minimal' \
      || echo "  tests failed in $project (coverage below is still valid)"
done

# Informational only: Risebird.Repository tops out near 7% under unit tests by construction.
echo
echo "Running Risebird.Repository.Tests (informational)"
dotnet test "$root/Risebird.Repository.Tests/Risebird.Repository.Tests.csproj" \
    --settings "$root/coverage.repository.runsettings" \
    --logger 'console;verbosity=minimal' \
  || echo "  tests failed in Risebird.Repository.Tests (coverage below is still valid)"

echo
echo "=================== COVERAGE BY PROJECT ==================="
python3 - "$root" <<'PY'
import glob, sys, xml.etree.ElementTree as ET
from collections import defaultdict

root = sys.argv[1]
agg = defaultdict(lambda: [0, 0, 0, 0])   # lines covered/valid, branches covered/valid

for path in glob.glob(f"{root}/**/coverage.cobertura.xml", recursive=True):
    tree = ET.parse(path)
    for package in tree.iter('package'):
        name = package.get('name')
        for line in package.iter('line'):
            agg[name][1] += 1
            if int(line.get('hits', '0')) > 0:
                agg[name][0] += 1
            cc = line.get('condition-coverage')
            if line.get('branch') == 'true' and cc and '(' in cc:
                covered, valid = cc.split('(')[1].rstrip(')').split('/')
                agg[name][2] += int(covered)
                agg[name][3] += int(valid)

if not agg:
    print("No coverage files produced.")
    sys.exit(1)

GATE = {"Risebird.Business", "Risebird.CommonHelper"}

for name in sorted(agg):
    lc, lv, bc, bv = agg[name]
    lp = 100.0 * lc / lv if lv else 0.0
    bp = 100.0 * bc / bv if bv else 0.0
    tag = "" if name in GATE else "   [informational]"
    print(f"{name:<28} lines {lp:6.2f}%  ({lc}/{lv})   branches {bp:6.2f}%  ({bc}/{bv}){tag}")

print()
print("Gate = Risebird.Business + Risebird.CommonHelper. Risebird.Repository is reported for")
print("information only; its unit-test ceiling is ~7% by construction (see COVERAGE_PLAN.md).")
PY

if [[ "${1:-}" == "--html" ]]; then
    echo
    echo "Generating HTML report..."
    inputs=$(find "$root" -name coverage.cobertura.xml | paste -sd ';' -)
    reportgenerator "-reports:$inputs" "-targetdir:$root/TestResults/CoverageReport" '-reporttypes:Html;TextSummary'
    echo "Report: $root/TestResults/CoverageReport/index.html"
fi
