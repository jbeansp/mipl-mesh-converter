using System;
using Nito.AsyncEx;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MiplMeshToObj
{
	class RunExternalProcess
	{

		public struct Result
		{
			public bool success;
			public string output;
			public string error;

			public static readonly Result fail = new Result()
			{
				success = false,
				output = "",
				error = ""
			};
		}


		public static Task<Result> RunAsync(
		string command,
		string arguments,
		CancellationToken cancellationToken,
		bool printStdout = false,
		bool printStderr = true)
		{
			int timeoutMsec = -1;
			return RunProcessHelperAsync(command, arguments, timeoutMsec, cancellationToken, printStdout, printStderr);
		}


		// use timeoutMsec = -1 for no timeout.
		private static async Task<Result> RunProcessHelperAsync(
			string command,
			string arguments,
			int timeoutMsec,
			CancellationToken cancellationToken,
			bool printStdout,
			bool printStderr)
		{

			try
			{
				Logger.Log("RunProcessHelperAsync(): {0} {1}", command, arguments);
				Process process = new Process();
				process.StartInfo.FileName = command;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.Start();
				//string output = p.StandardOutput.ReadToEnd();
				//string stderr = p.StandardError.ReadToEnd();

				// I'm getting some random hangs on StandardOutput.ReadToEnd()
				// using this solution:  https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
				// converted to use Async wait handles

				StringBuilder output = new StringBuilder();
				StringBuilder error = new StringBuilder();

				AsyncAutoResetEvent outputWaitHandle = new AsyncAutoResetEvent(false);
				AsyncAutoResetEvent errorWaitHandle = new AsyncAutoResetEvent(false);

				process.OutputDataReceived += (sender, e) =>
				{
					if (e.Data == null)
					{
						outputWaitHandle.Set();
					}
					else
					{
						output.AppendLine(e.Data);
					}
				};

				process.ErrorDataReceived += (sender, e) =>
				{
					if (e.Data == null)
					{
						errorWaitHandle.Set();
					}
					else
					{
						error.AppendLine(e.Data);
					}
				};

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				bool waitForExitResult = false;
				bool outputWaitHandleResult = false;
				bool errorWaitHandleResult = false;
				if (timeoutMsec > 0)
				{
					waitForExitResult = await Task.Run(() =>
					{
						try
						{
							return process.WaitForExit(timeoutMsec);
						}
						catch (TaskCanceledException)
						{
							Logger.Log("Caught TaskCanceledException");
							return false;
						}
						catch (OperationCanceledException)
						{
							Logger.Log("Caught OperationCanceledException");
							return false;
						}
						catch (Exception e)
						{
							Logger.Error("Caught exception: {0}", e);
							return false;
						}

					}, cancellationToken).ConfigureAwait(false);

					//make a timeout cancellationToken and link it with our other token, so they could both cancel.
					using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeoutMsec))
					{
						using (CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken))
						{
							await outputWaitHandle.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);
							outputWaitHandleResult = !linkedTokenSource.Token.IsCancellationRequested;

							await errorWaitHandle.WaitAsync(linkedTokenSource.Token).ConfigureAwait(false);
							errorWaitHandleResult = !linkedTokenSource.Token.IsCancellationRequested;
						}
					}
				}
				else
				{

					await Task.Run(() =>
					{
						try
						{
							process.WaitForExit();
							waitForExitResult = true;
						}
						catch (TaskCanceledException)
						{
							Logger.Log("Caught TaskCanceledException");
							waitForExitResult = false;
						}
						catch (OperationCanceledException)
						{
							Logger.Log("Caught OperationCanceledException");
							waitForExitResult = false;
						}
						catch (Exception e)
						{
							Logger.Error("Caught exception: {0}", e);
							waitForExitResult = false;
						}


					}, cancellationToken).ConfigureAwait(false);


					await outputWaitHandle.WaitAsync(cancellationToken).ConfigureAwait(false);
					outputWaitHandleResult = !cancellationToken.IsCancellationRequested;

					await errorWaitHandle.WaitAsync(cancellationToken).ConfigureAwait(false);
					errorWaitHandleResult = !cancellationToken.IsCancellationRequested;
				}


				if (waitForExitResult &&
					outputWaitHandleResult &&
					errorWaitHandleResult)
				{
					int exitCode = process.ExitCode;
					process.Dispose();

					//trim() because I notice there are /n at the end of output.
					string stdout = output.ToString().Trim();
					if (printStdout && !String.IsNullOrWhiteSpace(stdout))
					{
						Logger.Log("RunProcess() {0} {1} : stdout: {2}", command, arguments, stdout);
					}

					string stderr = error.ToString().Trim();
					if (printStderr && !String.IsNullOrWhiteSpace(stderr))
					{
						Logger.Log("RunProcess() {0} {1} : stderr: {2}", command, arguments, stderr);
					}

					if (exitCode != 0)
					{
						Logger.Error("RunProcess() returned an error code. \n Command: {0} {1}\n Output: {2}\n Stderr: {3}", command, arguments, output.ToString(), stderr);
						return Result.fail;
					}

					Result result = new Result()
					{
						success = true,
						output = stdout,
						error = stderr
					};

					return result;
				}
				else
				{
					Logger.Error("RunProcess() {0} {1} : Failed. TimeoutMsec set to {2}.  waitForExitResult {3}, outputWaitHandleResult {4}, errorWaitHandleResult {5}",
						command, arguments, timeoutMsec, waitForExitResult, outputWaitHandleResult, errorWaitHandleResult);
					return Result.fail;
				}

			}
			catch (Exception e)
			{
				Logger.Error("Caught exception: {0}", e);
				return Result.fail;
			}
			
		}
	}
}
