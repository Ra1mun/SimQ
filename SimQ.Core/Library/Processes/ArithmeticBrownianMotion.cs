using SimQCore.Processes;

namespace SimQCore.Library.Processes {
    class ArithmeticBrownianMotion: IProcess {
        private WienerProcess _wienerProcess;
        private double _mu;
        private double _sigma;
        private double _lastValue = 0;
        public ArithmeticBrownianMotion( double mu, double sigma ) {
            _wienerProcess = new WienerProcess();
            _mu = mu;
            _sigma = sigma;
        }

        public double Generate() {
            _lastValue += _sigma * _wienerProcess.Generate() + _mu;
            return _lastValue;
        }
    }
}
