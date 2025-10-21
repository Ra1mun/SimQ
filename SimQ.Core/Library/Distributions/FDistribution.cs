using System;

namespace SimQCore.Library.Distributions {
    public class FDistribution: IDistribution {
        private double _a;
        private double _b;
        private GammaDistribution _gammaDistribution1;
        private GammaDistribution _gammaDistribution2;
        private GammaDistribution _gammaDistribution3;
        public FDistribution( double a, double b ) {
            _a = a;
            _b = b;
            _gammaDistribution1 = new GammaDistribution( ( a + b ) / 2 );
            _gammaDistribution2 = new GammaDistribution( a / 2 );
            _gammaDistribution3 = new GammaDistribution( b / 2 );
        }

        public double Generate() {
            return _gammaDistribution1.Generate() * Math.Pow( _a / _b, _a / 2 ) /
                   ( _gammaDistribution2.Generate() * _gammaDistribution3.Generate() );
        }
    }
}
