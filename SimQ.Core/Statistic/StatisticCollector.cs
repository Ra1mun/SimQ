using SimQCore.Modeller.Models;

namespace SimQ.Core.Statistic {
    public class StatisticCollector {
        //private List<IModellingAgent> agents;

        public Dictionary<IModellingAgent, double> average;
        public Dictionary<IModellingAgent, Dictionary<int, double>> states;

        StatesStatistic StatesStat;


        //public Dictionary<IModellingAgent, double> variance;
        //public int[][] hist;
        //public double[][] cov;
        //public Dictionary<IModellingAgent, Dictionary<int, double>> empDist { get => states; }


        public void CollectStatistic( DataCollector data ) {
            //_id = Guid.NewGuid().ToString( "N" );
            //Date = DateTime.Now;
 
            //totalTime = data.totalTime;
            //totalCalls = modeller.data.totalCalls; // зачем?
            //totalStates = modeller.data.totalStates;

            //agents = modeller.problem.Agents;
            StatesStat = new StatesStatistic(data);
            states = StatesStat.states;

            CalculateAverages(ref average, states);
        }


        

        // скорее всего, не нужен
        /*public static int [] GetMaxCallArray( List<int> [] st, int total ) {
            int[] maxCallArray = new int[st.Length];
            int maxCall;
            for( int i = 0; i < st.Length; i++ ) {
                maxCall = 0;
                for( int j = 0; j < total; j++ ) {
                    if( st [i] [j] > maxCall ) {
                        maxCall = st [i] [j];
                    }
                }
                maxCallArray [i] = maxCall;
            }
            return maxCallArray;
        }*/

        private void CalculateAverages(ref Dictionary<IModellingAgent, double> average, Dictionary<IModellingAgent, Dictionary<int, double>> states) {
            average = new ();
            foreach(IModellingAgent agent in states.Keys) { 
                average.Add(agent, states[agent].Average(s => s.Key * s.Value));
            }
        }

        // потом исправить
        /*public void GetCovariance() {
            if( average == null ) {
                GetAverage();
            }

            double sum, item;
            int nodes = states.Length;
            cov = new double [nodes] [];
            for( int i = 0; i < nodes; i++ ) {
                cov [i] = new double [nodes];
            }

            for( int i = 0; i < nodes; i++ ) {
                for( int j = i; j < nodes; j++ ) {
                    sum = 0;
                    for( int k = 0; k < totalStates; k++ ) {
                        item = ( states [i] [k] - average [j] ) * ( states [j] [k] - average [j] );
                        sum += item;
                    }
                    cov [i] [j] = sum / ( totalStates - 1 );
                    cov [j] [i] = cov [i] [j];
                }
            }
        }*/

        // скорее всего, не нужен
        /*public void GetHistogram( int bins ) {
            int[] binsArray = new int[states.Length];
            int[] maxCallArray = GetMaxCallArray(states, totalStates);
            for( int i = 0; i < maxCallArray.Length; i++ ) {
                binsArray [i] = bins > maxCallArray [i] ? maxCallArray [i] : bins;
            }

            int step, currentStep, stepCount;
            hist = new int [states.Length] [];
            for( int i = 0; i < states.Length; i++ ) {
                step = maxCallArray [i] / binsArray [i];
                hist [i] = new int [binsArray [i]];
                for( int j = 0; j < totalStates; j++ ) {
                    currentStep = 0;
                    stepCount = 0;
                    while( states [i] [j] > ( currentStep + step ) ) {
                        currentStep += step;
                        stepCount++;
                    }

                    if( stepCount > binsArray [i] - 1 ) {
                        stepCount = binsArray [i] - 1;
                    }

                    hist [i] [stepCount]++;
                }
            }
        }*/

        // скорее всего, не нужен
        /*public void GetEmpiricalDistribution() {
            int[] maxCallArray = GetMaxCallArray(states, totalStates);

            double sum;
            empDist = new double [states.Length] [];
            for( int i = 0; i < states.Length; i++ ) {
                empDist [i] = new double [maxCallArray [i] + 1];
                for( int j = 0; j < empDist [i].Length; j++ ) {
                    sum = 0;
                    for( int k = 0; k < totalStates; k++ ) {
                        if( states [i] [k] <= j ) {
                            sum++;
                        }
                    }
                    empDist [i] [j] = sum / totalStates;
                }
            }
        }*/

        // потом доделать
        /*public void GetVariance() {
            if( average == null ) {
                GetAverage();
            }

            double sum;
            variance = new double [states.Length];
            for( int i = 0; i < variance.Length; i++ ) {
                sum = 0;
                foreach( int el in states [i] ) {
                    sum += ( el - average [i] ) * ( el - average [i] );
                }
                variance [i] = sum / ( totalStates - 1 );
            }
        }*/

        public void PrintAverage() {
            Console.WriteLine();
            if( average == null || average.Count == 0 ) {
                Console.WriteLine( "Средние значения не определены." );
            } else {
                Console.WriteLine( "Средние значения:" );
                foreach(IModellingAgent agent in average.Keys) {
                    Console.WriteLine($"{agent.Id}: {average[agent]}");
                }
                Console.WriteLine();
            }
        }

        // потом
        /*public void PrintVariance() {
            Console.WriteLine();
            if( variance == null || variance.Length == 0 ) {
                Console.WriteLine( "Массив дисперсий не определён." );
            } else {
                Console.WriteLine( "Дисперсии:" );
                for( int i = 0; i < variance.Length; i++ ) {
                    Console.Write( string.Format( "{0:0.00000} ", variance [i] ) );
                }

                Console.WriteLine();
            }
        }*/

        // не надо?
        /*public void PrintHistogram() {
            Console.WriteLine();
            if( hist == null || hist.Length == 0 ) {
                Console.WriteLine( "Данные для гистограммы не определены." );
            } else {
                Console.WriteLine( "Гистограмма:" );
                for( int i = 0; i < hist.Length; i++ ) {
                    for( int j = 0; j < hist [i].Length; j++ ) {
                        Console.Write( hist [i] [j] + " " );
                    }
                    Console.WriteLine();
                }
            }
        }*/

        // потом
        /*public void PrintCovariance() {
            Console.WriteLine();
            if( cov == null || cov.Length == 0 ) {
                Console.WriteLine( "Ковариационная матрица не определена." );
            } else {
                Console.WriteLine( "Ковариационная матрица:" );
                for( int i = 0; i < cov.Length; i++ ) {
                    for( int j = 0; j < cov [i].Length; j++ ) {
                        Console.Write( string.Format( "{0:0.00000} ", cov[i][j] ) );
                    }
                }
            }
        }*/

        




        /** Метод выводит собственные результаты каждого агента. */
        // потом
        /*public void PrintAgentsResults() {
            foreach( IModellingAgent item in agents ) {
                if( item is IResultableModel ) {
                    Misc.Log( $"\nРезультаты агента {item.Id}:" );
                    Misc.Log( ( item as IResultableModel ).GetResult() );
                }
            }
        }*/

        /** Метод выводит данные результатов моделирования. */
        public void CalcAndPrintAll() {
            //GetCovariance();
            // GetHistogram( 10 );
            //GetEmpiricalDistribution();
            //GetVariance();

            PrintAverage();
            //PrintCovariance();
            // PrintHistogram();
            StatesStat.Print_EmpDist();
            //PrintVariance();

            //PrintAgentsResults();
        }




        //public static string LoadResultToJson(string id)
        //{
        //    if (Storage.collection.CollectionNamespace.CollectionName != "ResultDto") Storage.SetCurrentCollection("ResultDto");
        //    var doc = Storage.GetDocument(id);
        //    return doc;
        //}

        //public static StatisticCollector LoadResultToObject(string id)
        //{
        //    if (Storage.collection.CollectionNamespace.CollectionName != "ResultDto") Storage.SetCurrentCollection("ResultDto");
        //    var doc = Storage.GetDocument(id);
        //    var result = JsonConvert.DeserializeObject<StatisticCollector>(doc);
        //    return result;
        //}

        //public void SaveResult()
        //{
        //    if (Storage.collection.CollectionNamespace.CollectionName != "ResultDto") Storage.SetCurrentCollection("ResultDto");
        //    var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        //    Storage.CreateDocument(json);
        //}
    }
}