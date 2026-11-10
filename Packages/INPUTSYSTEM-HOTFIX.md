# Input System 1.19.0 — hotfix T050

This project embeds `com.unity.inputsystem` as editable UPM source. Unity remains
6000.4.3f1; the upstream package version remains 1.19.0. The embedded package takes
precedence over the registry dependency and makes the correction reproducible on
fresh checkouts, without modifying Library/PackageCache or a shared installation.

## Provenance and scope

- Original: Unity registry package `com.unity.inputsystem` 1.19.0, already resolved
  by this project; upstream repository revision recorded in its package.json:
  `ca8d898356596a91bda6505e506cc1a4bb3a9fa6`.
- Original `InputSystem/InputManagerStateMonitors.cs` SHA-256:
  `FF41C3AC41B491231FEED88B7B7E21FB980D99C23C98FE50B9691308BB4F5985`.
- Patched file SHA-256:
  `F1309287C0FC31E0CAA018DC8388EC5F89281F79FB37A381BAFCF1C00D054A60`.
- All original source, metadata, samples, documentation and LICENSE.md are kept.
  No compiled installation, test results or caches are embedded.
- The only upstream source change is in `SignalStateChangeMonitor`: call
  `SortStateChangeMonitorsIfNecessary(deviceIndex)` once before scanning listeners,
  replacing `SortStateChangeMonitorsIfNecessary(i)` inside the listener loop.
- Copyright and the Unity Companion License remain in the package's LICENSE.md.

The old loop treated a listener index as a device index. It could sort another
device after its signal bits were set, moving a removed/null listener beneath a
pending bit. Dispatch then threw in FireStateChangeNotifications (including the
exception logger's access to the null monitor). Sorting only the owning device,
before setting its bits, preserves the listener/signal association.

## Regression and maintenance

`InputMonitorOrderingRegressionTests` constructs a deferred monitor removal via
the public input event API, then invokes the package's internal initial-signal
path by reflection in the Editor. The registry package reproduces the null
reference; the patched package must notify the intended monitor exactly once and
leave the unrelated monitor untouched. No private reflection is used in gameplay.
The device count deliberately separates device indices from listener indices.

See `specs/002-polimento-lago/validation-report.md` for red/green runs, complete
suite results and actual Android hotplug retests. The adapter also defers touch
context changes out of device dispatch, independently covered by its own tests.

On a future Input System upgrade, compare this method against upstream and keep
the regression. Remove the local hotfix only after the replacement passes it,
the project suites and physical hotplug tests. Do not silently replace the
embedded package with the unpatched registry package.
