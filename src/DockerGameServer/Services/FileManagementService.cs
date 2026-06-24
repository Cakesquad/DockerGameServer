namespace DockerGameServer.Services
{
	public class DirectoryContent
	{
		public string RelativePath { get; set; }
		public List<DirectoryInfo> Folders { get; set; }
		public List<FileInfo> Files { get; set; }
	}
	public class DirectoryInfo
	{
		public string Name { get; set; }
	}
	public class FileInfo
	{
		public string Name { get; set; }
		public string Type { get; set; }
	}

	public class FileManagementService(IConfiguration configuration, FileService fileService)
	{
		private readonly string _serversPath = configuration["Servers:Path"] ?? throw new InvalidOperationException("Servers:Path is not configured.");

		public async Task<DirectoryContent> GetDirectoryContentAsync(Guid serverId, string path, bool GoToParent = false)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			var basePath = Path.GetFullPath(Path.Combine(_serversPath, serverId.ToString()));

			var serverPath = Path.GetFullPath(Path.Combine(_serversPath, path));

			if (!serverPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException("The provided path is outside the allowed directory.", nameof(path));

			if (!Directory.Exists(serverPath))
				throw new DirectoryNotFoundException($"The directory on path '{path}' does not exist.");

			if (GoToParent)
			{
				var parentDir = Directory.GetParent(serverPath).FullName;

				// Ensure the parent directory is still within the allowed servers path
				if (parentDir == null || !parentDir.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
					serverPath = basePath;
				else
					serverPath = parentDir;
			}

			return new DirectoryContent
			{
				RelativePath = Path.GetRelativePath(_serversPath, serverPath),
				Folders = await GetDirectoriesAsync(serverPath),
				Files = await GetFilesAsync(serverPath)
			};
		}

		#region Help functions

		private async Task<List<DirectoryInfo>> GetDirectoriesAsync(string path)
		{
			List<DirectoryInfo> dirs = new();
			var subDirs = Directory.GetDirectories(path);
			if (subDirs.Length > 0)
			{
				foreach (var dir in subDirs)
				{
					var dirInfo = new DirectoryInfo
					{
						Name = Path.GetFileName(dir)
					};
					dirs.Add(dirInfo);
				}
			}

			return dirs.OrderBy(d => d.Name).ToList();
		} 

		private async Task<List<FileInfo>> GetFilesAsync(string path)
		{
			List<FileInfo> files = new();
			var subFiles = Directory.GetFiles(path);
			if (subFiles.Length > 0)
			{
				foreach (var file in subFiles)
				{
					var fileInfo = new FileInfo
					{
						Name = Path.GetFileName(file),
						Type = Path.GetExtension(file)
					};
					files.Add(fileInfo);
				}
			}

			return files.OrderBy(f => f.Name).ToList();
		}

		#endregion
	}
}