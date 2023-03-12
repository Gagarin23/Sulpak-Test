using System;
using System.Threading.Tasks;

namespace Application.Common.Options;

public static class StaticOptions
{
    public static readonly ParallelOptions ParallelOptions = new ParallelOptions()
    {
        //Если ядро окажется одно, то при делении на 2 получим 0, поэтому округление вверх
        MaxDegreeOfParallelism = (int)Math.Ceiling(Environment.ProcessorCount / 2d)
    };
}
