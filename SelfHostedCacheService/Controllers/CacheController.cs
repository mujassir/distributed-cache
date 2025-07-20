using Microsoft.AspNetCore.Mvc;
using SelfHostedCacheService.Services;
using System.Text.RegularExpressions;
using System.Linq;

namespace SelfHostedCacheService.Controllers
{
    [ApiController]
    [Route("cache")]
    public class CacheController : ControllerBase
    {
        private readonly CacheManager _cacheManager;

        public CacheController(CacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        [HttpPost]
        public IActionResult Add([FromBody] CacheAddRequest req)
        {
            _cacheManager.Add(req.Key, req.Value, req.Ttl);
            return Ok();
        }

        [HttpGet("{key}")]
        public IActionResult Get(string key)
        {
            var value = _cacheManager.Get(key);
            if (value == null) return NotFound();
            return Ok(value);
        }

        [HttpDelete("{key}")]
        public IActionResult Delete(string key)
        {
            if (_cacheManager.Remove(key)) return Ok();
            return NotFound();
        }

        [HttpPost("flush")]
        public IActionResult Flush()
        {
            _cacheManager.Flush();
            return Ok();
        }

        [HttpGet("exists/{key}")]
        public IActionResult Exists(string key)
        {
            return Ok(_cacheManager.Exists(key));
        }

        [HttpGet("stats")]
        public IActionResult Stats()
        {
            return Ok(new
            {
                Hits = _cacheManager.Hits,
                Misses = _cacheManager.Misses,
                KeyCount = _cacheManager.KeyCount
            });
        }

        [HttpGet("all")]
        public IActionResult All()
        {
            return Ok(_cacheManager.GetAll());
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string pattern)
        {
            return Ok(_cacheManager.Search(pattern));
        }
    }

    public class CacheAddRequest
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public int Ttl { get; set; } = 0;
    }
}