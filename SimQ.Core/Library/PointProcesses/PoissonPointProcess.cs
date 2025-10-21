using SimQCore.Library.Distributions;

namespace SimQCore.Library.PointProcesses {
    public class PoissonPointProcess: IPointProcess {
        private ExponentialDistribution _exponentialDistribution;
        private double _lastTime = 0;

        public PoissonPointProcess( double rate ) {
            _exponentialDistribution = new ExponentialDistribution( rate );
        }

        public double Generate() {
            _lastTime += _exponentialDistribution.Generate();
            return _lastTime;
        }
    }
}
