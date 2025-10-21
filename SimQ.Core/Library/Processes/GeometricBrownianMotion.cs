using SimQCore.Processes;
using System;

namespace SimQCore.Library.Processes {
    class GeometricBrownianMotion: IProcess {
        private WienerProcess _wienerProcess;
        private double _mu;
        private double _sigma;
        private double _lastValue = 1;

        public GeometricBrownianMotion( double mu, double sigma ) {
            _wienerProcess = new WienerProcess();
            _mu = mu;
            _sigma = sigma;
        }

        public double Generate() {
            _lastValue *= Math.Exp( ( _mu - ( _sigma * _sigma ) / 2 ) + _sigma * _wienerProcess.Generate() );
            return _lastValue;
        }
    }
}
