using System;

namespace SimQCore.Library.Distributions {
    public class TDistribution: IDistribution {
        private double _a;
        private GammaDistribution _gammaDistribution;
        private NormalDistribution _normalDistribution;
        public TDistribution( double a ) {
            _a = a;
            _normalDistribution = new NormalDistribution( 0, 1 );
            _gammaDistribution = new GammaDistribution( a / 2 );
        }

        public double Generate() {
            return ( ( Math.Sqrt( 2 * _a ) * _normalDistribution.Generate() ) / Math.Sqrt( _gammaDistribution.Generate() ) );
        }
    }
}
