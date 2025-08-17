using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NuitsJp.GistGet.Infrastructure.WinGet;

/// <summary>
/// WinGetのストリーミングパススルークライアント実装
/// </summary>
public class WinGetPassthroughClient(ILogger<WinGetPassthroughClient> logger) : IWinGetPassthroughClient
{

    /// <summary>
    /// winget.exeを実行し、引数をそのまま渡してストリーミング処理を行う
    /// </summary>
    /// <param name="args">wingetコマンド引数</param>
    /// <returns>wingetの終了コード</returns>
    public async Task<int> ExecuteAsync(string[] args)
    {
        var inputEncoding = Console.InputEncoding;
        var outputEncoding = Console.OutputEncoding;

        try
        {
            // Console.OutputEncodingをUTF-8に一時変更
            Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            logger.LogDebug("Console.OutputEncoding temporarily set to: {EncodingName}", Console.OutputEncoding.EncodingName);

            // winget.exeは常にUTF-8で出力するため、明示的にUTF-8を指定
            var psi = new ProcessStartInfo
            {
                FileName = "winget.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = false,
            };

            // 引数を設定
            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }

            using var proc = new Process();
            proc.StartInfo = psi;
            proc.EnableRaisingEvents = false;

            proc.Start();

            // ReSharper disable AccessToDisposedClosure
            // 標準出力と標準エラーを並列でストリーミング処理
            var stdOutTask = Task.Run(() => PumpAsync(proc.StandardOutput, Console.Out));
            var stdErrTask = Task.Run(() => PumpAsync(proc.StandardError, Console.Error));
            // ReSharper restore AccessToDisposedClosure

            await proc.WaitForExitAsync();
            await Task.WhenAll(stdOutTask, stdErrTask);

            return proc.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            logger.LogError(ex, "winget.exe の起動に失敗しました: {Message}", ex.Message);
            await Console.Error.WriteLineAsync($"winget.exe の起動に失敗しました: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "エラーが発生しました: {Message}", ex.Message);
            await Console.Error.WriteLineAsync($"エラーが発生しました: {ex}");
            return 1;
        }
        finally
        {
            // 元のエンコーディングに復元
            Console.InputEncoding = inputEncoding;
            Console.OutputEncoding = outputEncoding;
            logger.LogDebug("Console encodings restored to original values");
        }
    }

    /// <summary>
    /// 非同期で読み取りしながらコンソールへ書き出す（バッファで転送）
    /// </summary>
    /// <param name="reader">読み取り元</param>
    /// <param name="writer">書き込み先</param>
    private static async Task PumpAsync(TextReader reader, TextWriter writer)
    {
        var buffer = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            await writer.WriteAsync(buffer.AsMemory(0, read));
            await writer.FlushAsync();
        }
    }
}