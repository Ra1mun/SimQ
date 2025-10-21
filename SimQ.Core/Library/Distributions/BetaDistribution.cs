namespace SimQCore.Library.Distributions {
    public class BetaDistribution: IDistribution {
        private GammaDistribution _gammaDistribution1;
        private GammaDistribution _gammaDistribution2;
        private BaseSensor _baseSensor;
        private double _alpha;
        private double _beta;

        public BetaDistribution( double alpha, double beta ) {
            _alpha = alpha;
            _beta = beta;
            _gammaDistribution1 = new GammaDistribution( alpha );
            _gammaDistribution2 = new GammaDistribution( beta );

            _baseSensor = new BaseSensor();
        }

        public double Generate() {
            var alphaGamma = _gammaDistribution1.Generate();
            var betaGamma = _gammaDistribution2.Generate();
            return alphaGamma / ( alphaGamma + betaGamma );
        }
    }
}
