using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Shell.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckDateController : ControllerBase
    {
        // Gross, don't use this...
        private readonly IConfiguration configuration;

        public CheckDateController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpGet]
        public ActionResult<CheckDateResponse> Get()
        {
            var frozenRanges = configuration["CodeFreezeOptions:FrozenRanges"];

            var ranges = frozenRanges.Split("&&").Select(CodeFreezeRange.Generate).ToList();            

            var okayToRelease = CheckDateRanges(DateTime.UtcNow, ranges);          

            return Ok(new CheckDateResponse
            {
                OkayToRelease = okayToRelease
            });
        }

        private bool CheckDateRanges(DateTime utcNow, List<CodeFreezeRange> frozenRanges)
        {
            var isOkay = true;

            foreach (var range in frozenRanges)
            {
                if (range.StartUtc <= utcNow && utcNow <= range.EndUtc)
                {
                    isOkay &= false;
                }
                else
                {
                    continue;
                }
            }

            return isOkay;
        }

        public sealed class CodeFreezeRange
        {
            public bool StartInclusive { get; }
            public DateTime StartUtc { get; }
            public bool EndInclusive { get; }
            public DateTime EndUtc { get; }

            // TODO: configure whitelisting and blacklisting

            private CodeFreezeRange(bool startInclusive, DateTime startUtc, bool endInclusive, DateTime endUtc)
            {
                StartInclusive = startInclusive;
                StartUtc = startUtc;
                EndInclusive = endInclusive;
                EndUtc = endUtc;
            }

            public static CodeFreezeRange Generate(string fromString)
            {
                // TODO: actually check inclusive vs exclusive

                var dates = fromString.Substring(1, fromString.Length - 2).Split("--").Select(s => 
                        DateTime.ParseExact(s, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
                    )
                    .ToList();

                if(dates.Count == 1)
                {
                    var startOfDay = dates[0].Date;
                    var endOfDay = dates[0].Date.AddDays(1).AddMilliseconds(-1);

                    return new CodeFreezeRange(false, startOfDay, false, endOfDay);
                }
                else
                {
                    return new CodeFreezeRange(false, dates[0], false, dates[1]);
                }                
            }

            private static bool ExclusiveCharacter(char character)
            {
                switch (character)
                {
                    case '(':
                    case ')':
                        return true;
                    default:
                        return false;
                }
            }

            private static bool InclusiveCharacter(char character)
            {
                switch (character)
                {
                    case '[':
                    case ']':
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
