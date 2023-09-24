namespace RegionKit;

internal static class Logfix
{
	private const int MAX_BUFFERED_LINES = 4096;
	private static System.Collections.Concurrent.ConcurrentQueue<BufferedLogMessage> __bufferedLogMessages = new();
	private static int __discardedLogMessageCount = 0;
	internal static bool __writeTrace = true;
	internal static LevelLogCallback LogTrace { get; private set; } = (data) =>
	{
		if (!__writeTrace) return;
		__DefaultImpl_Log(BepInEx.Logging.LogLevel.Debug, $"[TRACE]:{data}");
	};
	internal static LevelLogCallback LogDebug { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Debug, data);
	internal static LevelLogCallback LogInfo { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Info, data);
	internal static LevelLogCallback LogMessage { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Message, data);
	internal static LevelLogCallback LogWarning { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Warning, data);
	internal static LevelLogCallback LogError { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Error, data);
	internal static LevelLogCallback LogFatal { get; private set; } = (data) => __DefaultImpl_Log(BepInEx.Logging.LogLevel.Fatal, data);
	internal static GeneralLogCallback Log { get; private set; } = __DefaultImpl_Log;

	internal static void __SwitchToBepinexLogger(BepInEx.Logging.ManualLogSource logger)
	{
		LogTrace = (data) =>
		{
			if (!__writeTrace) return;
			LogDebug($"[TRACE]:{data}");
		};
		LogDebug = logger.LogDebug;
		LogInfo = logger.LogInfo;
		LogMessage = logger.LogMessage;
		LogWarning = logger.LogWarning;
		LogError = logger.LogError;
		LogFatal = logger.LogFatal;
		Log = logger.Log;
		if (__bufferedLogMessages.Count is 0)
		{
			return;
		}
		LogWarning($"Detected buffered log lines! Count: {__bufferedLogMessages.Count}, max {MAX_BUFFERED_LINES}, discarded {__discardedLogMessageCount}");
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
	internal static string __GenerateLogString_Unity(BepInEx.Logging.LogLevel level, object data) => $"[POM/{level}] [{DateTime.UtcNow.TimeOfDay}] {data}";
	internal static string __GenerateLogString_Bepinex(BepInEx.Logging.LogLevel level, object data) => $"{data}";
	internal delegate void LevelLogCallback(object data);
	internal delegate void GeneralLogCallback(BepInEx.Logging.LogLevel level, object data);
	private record struct BufferedLogMessage(BepInEx.Logging.LogLevel level, string text, DateTime when);
}