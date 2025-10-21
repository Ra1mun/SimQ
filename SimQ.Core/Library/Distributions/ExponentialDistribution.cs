using System;

namespace SimQCore.Library.Distributions {
    public class ExponentialDistribution: IDistribution {
        private double[] stairWidth = new double[257];
        private double[] stairHeight = new double[256];
        private const double x1 = 7.69711747013104972;
        private const double A = 3.9496598225815571993e-3;
        private BaseSensor _baseSensor;
        private double rate;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="rate">Среднее число наступлений события в единицу времени</param>
        public ExponentialDistribution( double rate = 0 ) {
            this.rate = rate;
            stairHeight [0] = Math.Exp( -x1 );
            stairWidth [0] = A / stairHeight [0];
            // implicit value for the top layer
            stairWidth [256] = 0;
            for( int i = 1; i <= 255; ++i ) {
                // such x_i that f(x_i) = y_{i-1}
                stairWidth [i] = -Math.Log( stairHeight [i - 1] );
                stairHeight [i] = stairHeight [i - 1] + A / stairWidth [i];
            }

            _baseSensor = new BaseSensor();
        }

        public double Generate() {
            return ExpZiggurat() / rate;
        }

        public double Generate( double rate ) {
            return ExpZiggurat() / rate;
        }

        private double ExpZiggurat() {
            int iter = 0;
            do {
                int stairId = (int)_baseSensor.Next(0,256) & 255;
                double x = _baseSensor.Next(0, stairWidth[stairId]); // get horizontal coordinate
                if( x < stairWidth [stairId + 1] ) /// if we are under the upper stair - accept
                    return x;
                if( stairId == 0 ) // if we catch the tail
                    return x1 + ExpZiggurat();
                if( _baseSensor.Next( stairHeight [stairId - 1], stairHeight [stairId] ) < Math.Exp( -x ) ) // if we are under the curve - accept
                    return x;
                // rejection - go back
            } while( ++iter <= 1e9 ); // one billion should be enough to be sure there is a bug
            return Double.NaN; // fail due to some error
        }
    }
}
