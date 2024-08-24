﻿
namespace RegionKit;

internal static class Logfix
{
	private const int MAX_BUFFERED_LINES = 4096;
	private static System.Collections.Concurrent.ConcurrentQueue<BufferedLogMessage> __bufferedLogMessages = new();
	private static int __discardedLogMessageCount = 0;
	internal static bool __writeTrace = false;
	internal static bool __writeCallsiteInfo = false;

	internal static void LogTrace(
		object data,
		[System.Runtime.CompilerServices.CallerFilePath] string? _callerFilePath = null,
		[System.Runtime.CompilerServices.CallerLineNumber] int _callerLineNumber = 0,
		[System.Runtime.CompilerServices.CallerMemberName] string? _callerMemberName = null)
	{
		__Impl_LogTrace(__WrapDataWithCSInfo(data, _callerFilePath, _callerLineNumber, _callerMemberName));
	}
	internal static LevelLogCallback __Impl_LogTrace { get; private set; } = (data) =>
	{
		if (!__writeTrace) return;
		__DefaultImpl_Log(BepInEx.Logging.LogLevel.Debug, $"[TRACE]:{data}");
	};
	internal static void LogDebug(
		object data,
		[System.Runtime.CompilerServices.CallerFilePath] string? _callerFilePath = null,
		[System.Runtime.CompilerServices.CallerLineNumber] int _callerLineNumber = 0,
		[System.Runtime.CompilerServices.CallerMemberName] string? _callerMemberName = null)
	{
		__Impl_LogDebug(__WrapDataWithCSInfo(data, _callerFilePath, _callerLineNumber, _callerMemberName));
	}
	internal static LevelLogCallback __Impl_LogDebug { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Debug, data);
	internal static void LogInfo(
		object data,
		[System.Runtime.CompilerServices.CallerFilePath] string? _callerFilePath = null,
		[System.Runtime.CompilerServices.CallerLineNumber] int _callerLineNumber = 0,
		[System.Runtime.CompilerServices.CallerMemberName] string? _callerMemberName = null)
	{
		__Impl_LogInfo(__WrapDataWithCSInfo(data, _callerFilePath, _callerLineNumber, _callerMemberName));
	}
	internal static LevelLogCallback __Impl_LogInfo { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Info, data);
	internal static void LogMessage(
		object data,
		[System.Runtime.CompilerServices.CallerFilePath] string? _callerFilePath = null,
		[System.Runtime.CompilerServices.CallerLineNumber] int _callerLineNumber = 0,
		[System.Runtime.CompilerServices.CallerMemberName] string? _callerMemberName = null)
	{
		__Impl_LogMessage(__WrapDataWithCSInfo(data, _callerFilePath, _callerLineNumber, _callerMemberName));
	}
	internal static LevelLogCallback __Impl_LogMessage { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Message, data);
	internal static void LogfixWarning(
		object data,
		[System.Runtime.CompilerServices.CallerFilePath] string? _callerFilePath = null,
		[System.Runtime.CompilerServices.CallerLineNumber] int _callerLineNumber = 0,
		[System.Runtime.CompilerServices.CallerMemberName] string? _callerMemberName = null)
	{
		__Impl_LogWarning(__WrapDataWithCSInfo(data, _callerFilePath, _callerLineNumber, _callerMemberName));
	}
	internal static LevelLogCallback __Impl_LogWarning { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Warning, data);
	internal static void LogError(
		object data,
		[System.Runtime.CompilerServices.CallerFilePath] string? _callerFilePath = null,
		[System.Runtime.CompilerServices.CallerLineNumber] int _callerLineNumber = 0,
		[System.Runtime.CompilerServices.CallerMemberName] string? _callerMemberName = null)
	{
		__Impl_LogError(__WrapDataWithCSInfo(data, _callerFilePath, _callerLineNumber, _callerMemberName));
	}
	internal static LevelLogCallback __Impl_LogError { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Error, data);
	internal static void LogFatal(
		object data,
		[System.Runtime.CompilerServices.CallerFilePath] string? _callerFilePath = null,
		[System.Runtime.CompilerServices.CallerLineNumber] int _callerLineNumber = 0,
		[System.Runtime.CompilerServices.CallerMemberName] string? _callerMemberName = null)
	{
		__Impl_LogFatal(__WrapDataWithCSInfo(data, _callerFilePath, _callerLineNumber, _callerMemberName));
	}
	internal static LevelLogCallback __Impl_LogFatal { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Fatal, data);
	internal static void Log(
		BepInEx.Logging.LogLevel level,
		object data,
		[System.Runtime.CompilerServices.CallerFilePath] string? _callerFilePath = null,
		[System.Runtime.CompilerServices.CallerLineNumber] int _callerLineNumber = 0,
		[System.Runtime.CompilerServices.CallerMemberName] string? _callerMemberName = null)
	{
		__Impl_Log(level, __WrapDataWithCSInfo(data, _callerFilePath, _callerLineNumber, _callerMemberName));
	}
	internal static GeneralLogCallback __Impl_Log { get; private set; } = __DefaultImpl_Log;


	internal static void __SwitchToBepinexLogger(BepInEx.Logging.ManualLogSource logger)
	{
		__Impl_LogTrace = (data) =>
		{
			if (!__writeTrace) return;
			LogDebug($"[TRACE]:{data}");
		};
		__Impl_LogDebug = logger.LogDebug;
		__Impl_LogInfo = logger.LogInfo;
		__Impl_LogMessage = logger.LogMessage;
		__Impl_LogWarning = logger.LogWarning;
		__Impl_LogError = logger.LogError;
		__Impl_LogFatal = logger.LogFatal;
		__Impl_Log = logger.Log;
		if (__bufferedLogMessages.Count is 0)
		{
			return;
		}
		LogfixWarning($"Detected buffered log lines! Count: {__bufferedLogMessages.Count}, max {MAX_BUFFERED_LINES}, discarded {__discardedLogMessageCount}");
		while (__bufferedLogMessages.TryDequeue(out BufferedLogMessage result))
		{
			(BepInEx.Logging.LogLevel level, string data, DateTime when) = result;
			Log(level, $"[BUFFERED : {when}] {data}");
		}
		__discardedLogMessageCount = 0;

	}
	private static void __DefaultImpl_Log(BepInEx.Logging.LogLevel level, object data)
	{
		LevelLogCallback logAction = level switch
		{
			BepInEx.Logging.LogLevel.Error
			| BepInEx.Logging.LogLevel.Fatal
			=> UnityEngine.Debug.LogError,
			BepInEx.Logging.LogLevel.Warning
			=> UnityEngine.Debug.LogWarning,
			_ => UnityEngine.Debug.Log
		};
		logAction(__GenerateLogString_Unity(level, data));
		if (__bufferedLogMessages.Count >= MAX_BUFFERED_LINES)
		{
			__bufferedLogMessages.TryDequeue(out _);
			__discardedLogMessageCount++;
		}
		__bufferedLogMessages.Enqueue(new(level, __GenerateLogString_Bepinex(level, data), DateTime.Now));
	}
	internal static string __GenerateLogString_Unity(BepInEx.Logging.LogLevel level, object data) => $"[RK/{level}] [{DateTime.UtcNow.TimeOfDay}] {data}";
	internal static string __GenerateLogString_Bepinex(BepInEx.Logging.LogLevel level, object data) => $"{data}";
	private static object __WrapDataWithCSInfo(object data, string? callerFilePath, int callerLineNumber, string? callerMemberName)
	{
		return __writeCallsiteInfo ? $"@ {callerFilePath} : {callerLineNumber} ({callerMemberName}) : {data}" : data;
		//return $"@ {_callerFilePath} : {_callerLineNumber} ({_callerMemberName}) : {data}";
	}
	internal delegate void LevelLogCallback(object data);
	internal delegate void GeneralLogCallback(BepInEx.Logging.LogLevel level, object data);
	private record struct BufferedLogMessage(BepInEx.Logging.LogLevel level, string text, DateTime when);
}
