using Microsoft.AspNetCore.Mvc;

namespace DockerGameServer.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class FileController(IConfiguration configuration) : ControllerBase
	{
		private readonly string _serversPath = configuration["Servers:path"] ?? throw new InvalidOperationException("Servers:path is not configured.");

		[HttpPost("upload")]
		public async Task<ActionResult> UploadAsync([FromQuery] string path, [FromForm] List<IFormFile> files)
		{
			if (files == null || files.Count == 0)
				return BadRequest("No files were uploaded.");

			if (string.IsNullOrWhiteSpace(path))
				return BadRequest("Path is required.");

			if (!ResolveBasePath(path, out var basePath))
				return BadRequest("Invalid upload path.");

			Directory.CreateDirectory(basePath);

			foreach (var file in files)
			{
				if (!ResolveFilePath(basePath, file.FileName, out var fullFilePath))
					return BadRequest("Invalid file path.");

				var directory = Path.GetDirectoryName(fullFilePath);
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				using var fs = System.IO.File.Create(fullFilePath);
				await file.CopyToAsync(fs);
			}

			return Ok(new
			{
				message = "Files uploaded successfully",
				count = files.Count,
				savedTo = basePath
			});
		}

		#region Help functions

		private bool ResolveBasePath(string requestPath, out string resolvedBasePath)
		{
			resolvedBasePath = null;

			if (string.IsNullOrWhiteSpace(requestPath))
				return false;

			var fullPath = Path.GetFullPath(Path.Combine(_serversPath, requestPath));
			if (!fullPath.StartsWith(_serversPath, StringComparison.OrdinalIgnoreCase))
			return false;

			resolvedBasePath = fullPath;
			return true;
		}

		private bool ResolveFilePath(string basePath, string relativeFileName, out string resolvedFilePath)
		{
			resolvedFilePath = null;

			if (string.IsNullOrWhiteSpace(relativeFileName))
				return false;

			relativeFileName = relativeFileName.Replace("\\", "/");

			var fullFilePath = Path.GetFullPath(Path.Combine(basePath, relativeFileName));
			if (!fullFilePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
				return false;

			resolvedFilePath = fullFilePath;
			return true;
		}

		#endregion
	}
}