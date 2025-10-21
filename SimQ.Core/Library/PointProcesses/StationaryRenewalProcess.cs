using SimQCore.Library.Distributions;

namespace SimQCore.Library.PointProcesses {
    public class StationaryRenewalProcess: IPointProcess {
        private double _lastTime;
        private IDistribution _distribution;

        public StationaryRenewalProcess( IDistribution distribution ) {
            _distribution = distribution;
        }

        public double Generate() {
            _lastTime += _distribution.Generate();
            return _lastTime;
        }
    }
}
