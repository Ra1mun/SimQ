using System;
using System.Collections.Generic;

namespace SimQCore.Library.Distributions {
    public class NormalDistribution: IDistribution {
        private double[] stairWidth = new double[257];
        private double[] stairHeight = new double[256];
        private const double x1 = 3.6541528853610088;
        private const double A = 4.92867323399e-3;
        private BaseSensor _baseSensor;
        private ExponentialDistribution _exponentialDistribution;
        private double mu;
        private double sigma;
        public int p = 0;
        public int m = 0;
        public List<double> arr = new List<double>();

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="mu">Коэффициент сдвига</param>
        /// <param name="sigma">Коэффициент масштаба</param>
        public NormalDistribution( double mu = 0, double sigma = 0 ) {
            // coordinates of the implicit rectangle in base layer
            stairHeight [0] = Math.Exp( -.5 * x1 * x1 );
            stairWidth [0] = A / stairHeight [0];
            // implicit value for the top layer
            stairWidth [256] = 0;
            for( int i = 1; i <= 255; ++i ) {
                // such x_i that f(x_i) = y_{i-1}
                stairWidth [i] = Math.Sqrt( -2 * Math.Log( stairHeight [i - 1] ) );
                stairHeight [i] = stairHeight [i - 1] + A / stairWidth [i];
            }

            _baseSensor = new BaseSensor();
            _exponentialDistribution = new ExponentialDistribution( 0.0 );
            this.mu = mu;
            this.sigma = sigma;
        }

        public double Generate() {
            var a = NormalZiggurat();
            arr.Add( a );
            return mu + a * sigma;
        }

        public double Generate( double mu, double sigma ) {
            return mu + NormalZiggurat() * sigma;
        }

        private double NormalZiggurat() {
            int iter = 0;
            do {
                var B = (int)_baseSensor.Next(-256, 256);
                if( ( int )B > 0 )
                    p++;
                else
                    m++;
                int stairId = (int)B & 255;
                double x = _baseSensor.Next(0.0, stairWidth[stairId]); // get horizontal coordinate
                if( x < stairWidth [stairId + 1] )
                    return ( B > 0 ) ? x : -x;
                if( stairId == 0 ) // handle the base layer
                {
                    double z = -1;
                    double y;
                    if( z > 0 ) // we don't have to generate another exponential variable as we already have one
                    {
                        x = _exponentialDistribution.Generate( x1 );
                        z -= 0.5 * x * x;
                    }
                    if( z <= 0 ) // if previous generation wasn't successful
                    {
                        do {
                            x = _exponentialDistribution.Generate( x1 );
                            y = _exponentialDistribution.Generate( 1 );
                            z = y - 0.5 * x * x; // we storage this value as after acceptance it becomes exponentially distributed
                        } while( z <= 0 );
                    }
                    x += x1;
                    return ( B > 0 ) ? x : -x;
                }
                // handle the wedges of other stairs
                if( _baseSensor.Next( ( int )stairHeight [stairId - 1], ( int )stairHeight [stairId] ) < Math.Exp( -.5 * x * x ) )
                    return ( B > 0 ) ? x : -x;
            } while( ++iter <= 1e9 ); /// one billion should be enough
            return Double.NaN; /// fail due to some error
        }
    }
}
