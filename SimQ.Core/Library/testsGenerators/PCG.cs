namespace SimQCore.Library.TestsGenerators {
    public class PCG {

        static ulong state = 0x4d595df4d0f33173;     // Or something seed-dependent
        const ulong multiplier = 6364136223846793005u;
        const ulong increment  = 1442695040888963407u;    // Or an arbitrary odd constant

        private uint rotr32( uint x, int r ) {
            return x >> r | x << ( -r & 31 );
        }

        public long Next( int a, int b ) {
            ulong x = state;
            int count = (int)(x >> 59);       // 59 = 64 - 5

            state = x * multiplier + increment;
            x ^= x >> 18;
            var result = rotr32((uint)(x >> 27), count);// 18 = (64 - 27)/2
            return a + result * ( b - a ) / uint.MaxValue; // 27 = 32 - 5
        }

        public double Next( double a, double b ) {
            ulong x = state;
            int count = (int)(x >> 59);       // 59 = 64 - 5

            state = x * multiplier + increment;
            x ^= x >> 18;
            var result = rotr32((uint)(x >> 27), count);// 18 = (64 - 27)/2
            return a + result * ( b - a ) / uint.MaxValue; // 27 = 32 - 5
        }

        public PCG( ulong seed ) {
            state = seed + increment;
        }

    }
}
