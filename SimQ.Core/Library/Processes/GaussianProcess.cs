using SimQCore.Library.Distributions;
using System;

namespace SimQCore.Processes {
    public class GaussianProcess: IProcess {
        private double _q;
        private double _sigma;
        private NormalDistribution _normalDistribution;
        private double _lastValue = 0;

        public GaussianProcess( double q, double sigma ) {
            _normalDistribution = new NormalDistribution( 0, 1 );
            _q = q;
            _sigma = sigma;
        }

        public double Generate() {
            _lastValue = _lastValue * Math.Exp( -1 * _q )
                         + _sigma * _normalDistribution.Generate() * Math.Sqrt( 1 - Math.Exp( -2 * _q ) );
            return _lastValue;
        }
    }
}
