using SimQCore.Library.Distributions;
using System;

namespace SimQCore.Library.PointProcesses {
    public class NonStationaryPoissonPointProcess: IPointProcess {
        private BaseSensor _baseSensor;
        private ExponentialDistribution _exponentialDistribution;
        private double _lastTime = 0;
        private Func<double, double> _rateFunc;
        private double _maxRate;

        public NonStationaryPoissonPointProcess( Func<double, double> rateFunc, double maxRate ) {
            _maxRate = maxRate;
            _rateFunc = rateFunc;
            _baseSensor = new BaseSensor( 0, maxRate );
            _exponentialDistribution = new ExponentialDistribution( maxRate );
        }

        public double Generate() {
            double rateValue;
            double u;
            double newTime;
            do {
                newTime = _lastTime + _exponentialDistribution.Generate();
                u = _baseSensor.Next();
                rateValue = _rateFunc( newTime );
            } while( rateValue < u );

            _lastTime = newTime;
            return _lastTime;
        }
    }
}
