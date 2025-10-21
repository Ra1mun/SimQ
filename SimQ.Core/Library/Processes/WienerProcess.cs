using SimQCore.Library.Distributions;

namespace SimQCore.Processes {
    public class WienerProcess: IProcess {
        private NormalDistribution _normalDistribution;
        private double _lastValue = 0;

        public WienerProcess() {
            _normalDistribution = new NormalDistribution( 0, 1 );
        }

        public double Generate() {
            _lastValue += _normalDistribution.Generate();
            return _lastValue;
        }
    }
}
