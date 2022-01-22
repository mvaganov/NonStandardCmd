using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using NonStandard.Commands;

namespace NonStandard.Cli {
	// TODO re-implement, so command-line events can be called from C# programs while maintaining command line state.
	public class SystemBridge
	{
		System.Diagnostics.Process system_process;
		Thread thread;
		FileInfo cmdLineSemaphoreFile;
		private string currentCommand = "";
		private Show.PrintFunc currentCommand_callback;
		/// the outputs from the bash thread
		private List<string> log, err;
		private bool isInitialized = false;
		/// used to communicate to the CmdLine that the bash thread needs to refresh the prompt
		private bool promptNeedsRedraw = false;
		/// used to communicate to the CmdLine that the bash thread finished something
		private bool probablyFinishedCommand = true;
		//private CmdLine_base _cmd;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || PLATFORM_STANDALONE_WIN
		private string commandExecutable = "cmd";
		private string activeDir = ".";
		private string tempFile() { return System.Environment.GetEnvironmentVariable("TEMP") + "\\.cmdLine"; }
		private string CommandToLockFile(string filename) { return "attrib +r " + filename; }
		private string CommandToUnlockFile(string filename) { return "attrib -r " + filename; }
#else
		private string commandExecutable = "/bin/bash";
		private string activeDir = null;
		private string tempFile() { return "/tmp/.cmdLine"; }
		private string CommandToLockFile(string filename) { return "chmod 0444 " + filename; }
		private string CommandToUnlockFile(string filename) { return "chmod 0775 " + filename; }
#endif

		private void BlockTillFile(string filename, bool writable, long timeout, string timeoutMessage) {
			long start = System.Environment.TickCount;//Clock.NowRealtime;
			timeout += start;
			while (((File.GetAttributes(filename) & FileAttributes.ReadOnly) != 0) == writable) {
				if (System.Environment.TickCount >= timeout) { throw new System.Exception(timeoutMessage); }
				Thread.Sleep(3);
				if (system_process.HasExited)
				{
					UnityEngine.Debug.LogWarning("process exited before file blocking operation could finish.");
					break;
				}
			}
			//long end = NS.Chrono.NowRealtime;
			//long deltaMS = end - start;
			//UnityEngine.Debug.Log(deltaMS+" waiting for "+filename+" to be "+(writable?"unlocked":"locked"));
		}

		public SystemBridge(string commandExecutable = null)
		{
			if (commandExecutable != null)
			{
				this.commandExecutable = commandExecutable;
			}
		}

		private void CleanupLockFile()
		{
			if (cmdLineSemaphoreFile != null)
			{
				cmdLineSemaphoreFile.Refresh();
				if (cmdLineSemaphoreFile.Exists)
				{
					cmdLineSemaphoreFile.Attributes = cmdLineSemaphoreFile.Attributes & ~FileAttributes.ReadOnly;
					cmdLineSemaphoreFile.Delete();
				}
			}
		}

		private void BlockOnFileLock(bool setLocked, List<string> outputToIgnore, string lockFileName, long timeoutMax, string timeoutMessage)
		{
			string lockCommand = setLocked ? CommandToLockFile(lockFileName) : CommandToUnlockFile(lockFileName);
			outputToIgnore.Add(lockCommand);
			system_process.StandardInput.WriteLine(lockCommand);
			system_process.StandardInput.Flush();
			BlockTillFile(lockFileName, !setLocked, timeoutMax, timeoutMessage);
		}
		
		public void Release() {
			// make sure the file is unlocked before deleting
			CleanupLockFile();
			// make sure the command line process has had exit called
			if (!system_process.HasExited)
			{
				system_process.StandardInput.WriteLine("exit");
				system_process.StandardInput.Flush();
			}
			System.Diagnostics.Process proc = system_process;
			Thread t = thread;
			//if (_cmd != null) { _cmd.NeedToRefreshUserPrompt = true; }
			thread = null;
			system_process = null;
			isInitialized = false;
			probablyFinishedCommand = true;
			proc.WaitForExit();
			proc.Close();
			t.Join(); // should be the last statement
		}

		public void DoCommand(string s, object whosAsking, Show.PrintFunc cb = null)//, CmdLine_base cmd = null)
		{
			if (thread == null)
			{
				//_cmd = cmd;
				currentCommand = s.Trim();
				currentCommand_callback = cb;
				log = new List<string>();
				err = new List<string>();
				thread = new System.Threading.Thread(delegate () {
					string commandName = commandExecutable;
					if(activeDir == null || activeDir == "")
					{
						activeDir = PWD();
					}
					system_process = new System.Diagnostics.Process {
						StartInfo = new ProcessStartInfo {
							FileName = commandName,
							Arguments = "",
							UseShellExecute = false,
							RedirectStandardOutput = true,
							RedirectStandardInput = true,
							RedirectStandardError = true,
							CreateNoWindow = true,
							WorkingDirectory = activeDir
						}
					};
					// find the semaphore file that is used to determine if a command is still running
					string tempF = tempFile();
					string myTempFile = tempF;
					int fileChecks = -1;
					do {
						if (fileChecks++ > 1000) { throw new System.Exception("could not create valid command line semaphore file"); }
						myTempFile = tempF + fileChecks;
						cmdLineSemaphoreFile = new System.IO.FileInfo(myTempFile);
					} while (cmdLineSemaphoreFile.Exists);
					StreamWriter sw = new StreamWriter(myTempFile); sw.Close();
					// make sure the file is unlocked (which signifies that a command is ready to execute)
					File.SetAttributes(myTempFile, File.GetAttributes(myTempFile) & ~FileAttributes.ReadOnly);

					system_process.Start();
					// output data to ignored
					List<string> outputToIgnore = new List<string>();
					// asynchronous callback for output. Needs a separate structure to manage what gets output (outputToIgnore)
					system_process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) {
						//UnityEngine.Debug.Log("processing '" + e.Data + "' vs ['"+string.Join("', '",outputToIgnore)+"']");
						if (outputToIgnore.Count > 0) {
							int ignoreIndex = outputToIgnore.FindIndex(str => e.Data.EndsWith(str));//outputToIgnore.IndexOf(e.Data);
							if(ignoreIndex >= 0) {
								//UnityEngine.Debug.Log("Ignored " + e.Data+" @" + ignoreIndex);
								outputToIgnore.RemoveAt(ignoreIndex);
								return;
							}
						}
						if (currentCommand_callback == null) {
							log.Add(e.Data);
							probablyFinishedCommand = true;
						} else {
							currentCommand_callback(e.Data);
							currentCommand_callback = null;
						}
					};
					system_process.BeginOutputReadLine();
					bool ignoreErrors = true;
					system_process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e) {
						if (ignoreErrors) { return; }
						err.Add(e.Data);
						probablyFinishedCommand = true;
					};
					system_process.BeginErrorReadLine();
					system_process.StandardInput.WriteLine(' '); // force an error, because the StandardInput has a weird character in it to start with
					system_process.StandardInput.Flush();

					isInitialized = true;
					promptNeedsRedraw = true;
					string lastCommand = null;
					while (!system_process.HasExited)
					{
						if (!string.IsNullOrEmpty(currentCommand))
						{
							ignoreErrors = false;
							probablyFinishedCommand = false;
							BlockOnFileLock(true, outputToIgnore, myTempFile, 1000, "setting up for "+currentCommand);

							system_process.StandardInput.WriteLine(currentCommand);
							system_process.StandardInput.Flush();

							if (!system_process.HasExited) {
								BlockOnFileLock(false, outputToIgnore, myTempFile, 1000 * 60 * 5, currentCommand+" took longer than 5 minutes");
							}
							probablyFinishedCommand = true;
							promptNeedsRedraw = true;
							lastCommand = currentCommand;
							currentCommand = "";
						}
						else { Thread.Sleep(10); }
					}
					Release();
				});
				thread.Start();
			} else {
				if (!string.IsNullOrEmpty(s)) {
					s += "\n";
					currentCommand = s;
					currentCommand_callback = cb;
				}
			}
		}

		private string COMMAND_LINE_GETTER(string call)
		{
			System.Diagnostics.Process proc = new System.Diagnostics.Process {
				StartInfo = new ProcessStartInfo {
					FileName = call,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				}
			};
			proc.Start();
			string r = proc.StandardOutput.ReadLine();
			return r;
		}
		public string PWD()
		{
			string pwd = COMMAND_LINE_GETTER("pwd");
			return pwd;
		}

		public bool IsProbablyIdle()
		{
			return (thread == null || (string.IsNullOrEmpty(currentCommand) && probablyFinishedCommand));
		}

		public bool IsInitialized() { return isInitialized; }

		public string MachineName { get { return system_process.MachineName; } }

		public void Update(Command.Exec inst)//, CmdLine_base cmd)
		{
			bool somethingPrinted = false;
			if (log != null)
			{
				while (log.Count > 0)
				{
					//cmd.HandleLog(log[0], "", CmdLine_base.LogType.Log);
					inst.print(log[0]);
					log.RemoveAt(0);
					somethingPrinted = true;
				}
			}
			if (err != null)
			{
				while (err.Count > 0)
				{
					//cmd.HandleLog(err[0], "", CmdLine_base.LogType.Error);
					inst.print(err[0]);
					err.RemoveAt(0);
					somethingPrinted = true;
				}
			}
			string s = null;
			if (inst != null)
			{
				s = inst.tok.GetString();
				if (s != null) { DoCommand(s, inst.src); }
			}
			if (string.IsNullOrEmpty(s) &&
			string.IsNullOrEmpty(currentCommand) &&
			(somethingPrinted || promptNeedsRedraw))
			{
				//cmd.NeedToRefreshUserPrompt = true;
			}
			//if (cmd.NeedToRefreshUserPrompt)
			//{
			//	promptNeedsRedraw = false;
			//}
		}
	}
}