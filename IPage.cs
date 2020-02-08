using System;

namespace Chaining
{
    public interface IPage
    {
        Action<string> Log { get; set; }
    }
}
