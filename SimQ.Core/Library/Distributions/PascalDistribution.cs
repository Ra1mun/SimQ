namespace SimQCore.Library.Distributions {
    public class PascalDistribution: IDistribution {
        private GammaDistribution _gammaDistribution;
        private PoissonDistribution _poissonDistribution;
        private BaseSensor _baseSensor;

        public PascalDistribution( double p, int r ) {
            _baseSensor = new BaseSensor();
            _gammaDistribution = new GammaDistribution( r, p / ( 1 - p ) );
            _poissonDistribution = new PoissonDistribution( 0.0 );
        }

        public double Generate() {
            return _poissonDistribution.Generate( _gammaDistribution.Generate() );
        }
    }
}
