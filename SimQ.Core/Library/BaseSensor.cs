using System;

namespace SimQCore.Library {
    public class BaseSensor {
        private const uint P1 = 2654435761;
        private const uint P2 = 2246822519;
        private const uint P3 = 3266489917;
        private const uint P4 = 668265263;
        private const uint P5 = 374761393;
        private const uint max = uint.MaxValue;
        public uint seed;
        private uint count;
        private double a;
        private double b;

        public BaseSensor( double a = 0.0, double b = 1.0 ) {
            //if (a > b)
            //    throw new Exception("В BaseSensor параметр a больше параметра b");
            //Thread.Sleep(1);
            seed = ( uint )DateTime.Now.Ticks;
            count = 0;
            this.a = a;
            this.b = b;
        }

        public BaseSensor( uint seed, double a = 0.0, double b = 1.0 ) {
            //if (a > b)
            //    throw new Exception("В BaseSensor параметр a больше параметра b");
            //Thread.Sleep(1);
            this.seed = seed;
            count = 0;
            this.a = a;
            this.b = b;
        }

        public double Next() {
            return a + CountUp() * ( b - a ) / max;
        }

        public double Next( uint count ) {
            return a + CountUp() * ( b - a ) / max;
        }

        public double Next( double a, double b ) {
            return a + CountUp() * ( b - a ) / max;
        }

        private double CountUp() {
            var h32 = seed + P5;
            h32 += 4U;
            h32 += count * P3;
            h32 = ( h32 >> 17 ) | ( h32 << ( 15 ) ) * P4;
            h32 ^= h32 >> 15;
            h32 *= P2;
            h32 ^= h32 >> 13;
            h32 *= P3;
            h32 ^= h32 >> 16;
            count++;
            return h32;
        }
    }
}
