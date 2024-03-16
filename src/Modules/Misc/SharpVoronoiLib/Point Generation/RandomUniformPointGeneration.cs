using System;

namespace SharpVoronoiLib
{
    internal class RandomUniformPointGeneration : RandomPointGeneration
    {
        protected override double GetNextRandomValue(System.Random random, double min, double max)
        {
            do
            {
                double value = min + random.NextDouble() * (max - min);
                
                if (value > min && value < max)
                    return value;
                
            } while (true);
        }
    }
}
