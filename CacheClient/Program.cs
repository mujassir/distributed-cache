using System;
using System.Threading.Tasks;

namespace CacheClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new CacheClient();
            string key = "testkey";
            string value = "Hello, Cache!";

            Console.WriteLine($"Adding key: {key}");
            bool added = await client.AddAsync(key, value, 60);
            Console.WriteLine($"Add result: {added}");

            Console.WriteLine($"Getting key: {key}");
            string getValue = await client.GetAsync(key);
            Console.WriteLine($"Value: {getValue}");

            Console.WriteLine($"Searching for keys with 'test'");
            string searchResult = await client.SearchAsync("test");
            Console.WriteLine($"Search result: {searchResult}");

            Console.WriteLine($"Deleting key: {key}");
            bool deleted = await client.DeleteAsync(key);
            Console.WriteLine($"Delete result: {deleted}");
        }
    }
}