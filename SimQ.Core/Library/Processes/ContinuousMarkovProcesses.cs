using SimQCore.Library;
using System;

namespace SimQCore.Processes {
    public class ContinuousMarkovProcesses: IProcess {
        private BaseSensor _baseSensor;
        private int _currPoint;
        private double[][] _matrixP;

        public ContinuousMarkovProcesses( double [] [] matrixQ, double [] probabilityPointsOnFirst ) {
            _baseSensor = new BaseSensor();

            var matrixP = new double[matrixQ.Length][];
            for( int i = 0; i < matrixP.Length; i++ ) {
                matrixP [i] = new double [matrixP.Length];
                for( int j = 0; j < matrixP.Length; j++ ) {
                    if( i == j )
                        continue;

                    matrixP [i] [j] = -1 * matrixQ [i] [j] / matrixQ [i] [i];
                }
            }

            var rand = _baseSensor.Next();
            var summP = 0.0;
            for( int i = 0; i < probabilityPointsOnFirst.Length; i++ ) {
                summP += probabilityPointsOnFirst [i];

                if( rand < summP ) {
                    _currPoint = i;
                    break;
                }
            }

            _matrixP = matrixP;
        }

        public double Generate() {
            var rndValue = _baseSensor.Next();
            var summP = 0.0;
            for( int i = 0; i < _matrixP [_currPoint].Length; i++ ) {
                summP += _matrixP [_currPoint] [i];

                if( rndValue < summP ) {
                    _currPoint = i;
                    return i;
                }
            }
            return Double.NaN;
        }
    }
}
