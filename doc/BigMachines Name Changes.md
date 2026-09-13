# BigMachines Name Changes

This release renames public APIs to follow .NET naming guidelines and make intent clearer. Runtime behavior is unchanged.

- **Source compatibility:** breaking. Update references using the tables below, then rebuild so the new source generator regenerates code.
- **Persisted data:** compatible. Root control keys and machine Tinyhand keys are unchanged, so existing snapshots and journals still load.
- **Generator/library pairing:** use the library and generator from the same release.

## Migration steps

1. Update the `BigMachines` package.
2. Apply the search/replace list in [Suggested replacements](#suggested-replacements).
3. Build, then fix the remaining errors using the tables below.
4. Rename members in your machines that now collide with generated names (see [Reserved member names](#reserved-member-names)).

## Types

| Old | New | Notes |
| --- | --- | --- |
| `BigMachineException` | `MachineExceptionInfo` | Not an `Exception`; holds `Machine` and `Exception`. |
| `ExceptionHandlerDelegate` | `MachineExceptionHandler` | Delegate. |
| `OperationalFlag` | `OperationalFlags` | `[Flags]` enum. |
| `CommandResult` (enum) | `CommandStatus` | `CommandResult<TResponse>` keeps its name. Command methods return `CommandStatus`, `CommandResult<TResponse>`, `Task<CommandStatus>`, or `Task<CommandResult<TResponse>>`. |
| `Machine.ManMachineInterface` | `Machine.MachineHandle` | Also `MachineHandle<TState>` and `MachineHandle<TIdentifier, TState>`. |
| Generated `<Machine>.Interface` | `<Machine>.Handle` | Nested handle class generated in each machine. |
| Generated `AllCommandExtension` | `AllCommandExtensions` | Static class for `All<Command>` extension methods. |
| Type parameter `TInterface` | `THandle` | `SingleMachineControl`, `UnorderedMachineControl`, `SequentialMachineControl`, `MultiMachineControl`. |

## Attribute properties

| Attribute | Old | New |
| --- | --- | --- |
| `BigMachineObjectAttribute` | `Inclusive` | `IncludeAllMachines` |
| `BigMachineObjectAttribute` | `RecursiveDetection` | `EnableRecursiveDetection` |
| `AddMachineAttribute<TMachine>` | `Volatile` | `NonPersistent` |
| `MachineObjectAttribute` | `StartByDefault` | `CreateOnStart` |
| `MachineObjectAttribute` | `NumberOfTasks` | `WorkerCount` |
| `MachineObjectAttribute` | `Private` | `ExcludeFromIncludeAllMachines` |
| `CommandMethodAttribute` | `All` | `GenerateAllCommand` |

## Members

| Type | Old | New | Notes |
| --- | --- | --- | --- |
| `BigMachineBase` | `GroupName` | `ExecutionGroupName` | Constant. |
| `BigMachineBase` | `GetArray()` | `GetControls()` | Generated roots override it. |
| `BigMachineBase` | `StartBigMachine()` | `OnStart()` | Protected virtual; generated roots override it. |
| `IBigMachine` | `LastRun` | `LastRunTime` | |
| `IBigMachine` | `CheckActiveMachine(Type? machineTypeToBeExcluded)` | `HasPendingWork(Type? excludedMachineType)` | |
| `IBigMachine` | `CheckRecursive(uint machineSerial, ulong id)` | `CheckCircularCommand(uint machineSerial, ulong commandId)` | |
| `IBigMachine` | `ProcessException()` | `ProcessExceptions()` | |
| `MachineRegistry` | `Get<TMachine>()` | `GetInformation<TMachine>()` | |
| `MachineRegistry` | `TryGet<TMachine>(out MachineInformation)` | `TryGetInformation<TMachine>(out MachineInformation)` | |
| `MachineInformation` | `Serializable` | `IsSerializable` | Positional record parameter. |
| `MachineInformation` | `NumberOfTasks` | `WorkerCount` | Positional record parameter. |
| `MachineControl` and all controls | `GetArray()` | `GetHandles()` | Returns handle snapshots. |
| `MultiMachineControl<TIdentifier, THandle>` | `AllRunAsync()` | `RunAllAsync()` | |
| All controls | `Prepare(BigMachineBase)` | `Attach(BigMachineBase)` | Normally called by generated code. |
| `ManualMachineControl` | `TryGet<TMachine>()` | `Find<TMachine>()` | Returns the handle or `null`. |
| `SequentialMachineControl<TIdentifier, TMachine, THandle>` | `TryGet(TIdentifier)` | `Find(TIdentifier)` | Returns the handle or `null`. |
| `SingleMachineControl`, `UnorderedMachineControl` | `CreateAlways(...)` | `CreateOrReplace(...)` | |
| `ISequentialMachineControl`, `SequentialMachineControl` | `GetFirst()` | `PeekFirst()` | |
| `Machine` | `DefaultTimeout` | `DefaultInterval` | Protected `init` property. |
| `Machine` | `InterfaceInstance` | `HandleInstance` | |
| `Machine` | `__interfaceInstance__` | `__handleInstance__` | Protected field used by generated code. |
| `Machine.MachineHandle` | `TerminateMachine()` | `Terminate()` | |
| `Machine.MachineHandle` | `PauseMachine()` | `Pause()` | |
| `Machine.MachineHandle` | `UnpauseMachine()` | `Resume()` | |
| `Machine.MachineHandle` | `GetDefaultTimeout()` | `GetDefaultInterval()` | |
| `CommandResult<TResponse>` | `Result` | `Status` | Field of type `CommandStatus`. |
| `CommandResult<TResponse>` | `Resnpose` | removed | Use `Response`. |
| `IdentifierAndCommandResult<TIdentifier>` | `Result` | `Status` | `IdentifierAndCommandResult<TIdentifier, TResponse>.Result` is unchanged. |
| Generated machine (open generic) | `RegisterBM()` | `RegisterMachine()` | Also the generated loader method. |

## Parameters

Parameter renames only affect named arguments and overrides.

| Members | Old | New |
| --- | --- | --- |
| `Machine.OnCreate`, `GetOrCreate`, `TryCreate`, `CreateOrReplace` (all controls) | `createParam` | `createParameter` |
| `SingleMachineControl.TryGet`, `UnorderedMachineControl.TryGet` | `machineInterface` | `handle` |
| `CommandResult<TResponse>` constructor, `IdentifierAndCommandResult<TIdentifier>` constructor | `result` | `status` |

## Reserved member names

Machines cannot declare members with generated names (diagnostic `BMG004`). The reserved set changed:

| Old | New |
| --- | --- |
| `Interface` | `Handle` |
| `CreateInterface` | `CreateHandle` |
| `RegisterBM` | `RegisterMachine` |

`State`, `Command`, `ChangeState`, and the other reserved names are unchanged.

## Suggested replacements

Apply these case-sensitive regular expressions in order, then review the results before building.

| # | Find (regex) | Replace | Review |
| --- | --- | --- | --- |
| 1 | `\bManMachineInterface\b` | `MachineHandle` | |
| 2 | `\bInterfaceInstance\b` | `HandleInstance` | |
| 3 | `\.Interface\b` | `.Handle` | Only generated machine handle types. |
| 4 | `\bUnpauseMachine\b` | `Resume` | |
| 5 | `\bPauseMachine\b` | `Pause` | |
| 6 | `\bTerminateMachine\b` | `Terminate` | |
| 7 | `\bCommandResult\b(?!<)` | `CommandStatus` | Keeps `CommandResult<TResponse>`. Do not apply to your own types named `CommandResult`. |
| 8 | `\bOperationalFlag\b` | `OperationalFlags` | |
| 9 | `\bBigMachineException\b` | `MachineExceptionInfo` | |
| 10 | `\bExceptionHandlerDelegate\b` | `MachineExceptionHandler` | |
| 11 | `\bDefaultTimeout\b` | `DefaultInterval` | |
| 12 | `\bCreateAlways\b` | `CreateOrReplace` | |
| 13 | `\bAllRunAsync\b` | `RunAllAsync` | |
| 14 | `\bGetFirst\b` | `PeekFirst` | Sequential controls only. |
| 15 | `\bInclusive\s*=` | `IncludeAllMachines =` | In `[BigMachineObject(...)]`. |
| 16 | `\bPrivate\s*=` | `ExcludeFromIncludeAllMachines =` | In `[MachineObject(...)]`. |
| 17 | `\bStartByDefault\s*=` | `CreateOnStart =` | In `[MachineObject(...)]`. |
| 18 | `\bNumberOfTasks\b` | `WorkerCount` | |
| 19 | `\bVolatile\s*=\s*(true\|false)` | `NonPersistent = $1` | In `[AddMachine<T>(...)]` only; do not touch `System.Threading.Volatile`. |
| 20 | `\bAll\s*=\s*(true\|false)` | `GenerateAllCommand = $1` | In `[CommandMethod(...)]` only. |
| 21 | `\bcreateParam\b` | `createParameter` | |

Resolve these manually, because the old names are ambiguous:

- `GetArray()`: on a root, use `GetControls()`; on a control, use `GetHandles()`.
- `.Result` on `CommandResult<TResponse>` or `IdentifierAndCommandResult<TIdentifier>`: use `.Status`. Leave `Task.Result` and `IdentifierAndCommandResult<TIdentifier, TResponse>.Result` unchanged.
- `TryGet(...)` returning a nullable handle (`ManualMachineControl`, `SequentialMachineControl`): use `Find(...)`. `TryGet(..., out ...)` on single and unordered controls is unchanged.
- `IBigMachine` members: `LastRun`, `CheckActiveMachine`, `CheckRecursive`, `ProcessException`.
- `MachineRegistry.Get` / `TryGet`, and control `Prepare(...)` calls, if you call them directly.
