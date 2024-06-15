using Microsoft.AspNetCore.Mvc;

namespace NovelsCollector.Core.Services
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("static")]
    public class DownloadController : ControllerBase
    {
        [HttpGet("{dayFolder}/{formatFolder}/{fileName}")]
        public async Task<IActionResult> GetExportFile([FromRoute] string dayFolder, [FromRoute] string formatFolder, [FromRoute] string fileName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", dayFolder, formatFolder, fileName);
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/octet-stream", fileName);
        }
    }
}
