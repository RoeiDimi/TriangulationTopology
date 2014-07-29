using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures;
using TriangulationTopology.Properties;

namespace TriangulationTopology
{
    public static class AlgorithmCalculations
    {
        private static double _epsilon = Double.Parse(Settings.Default.Properties["epsilon"].DefaultValue.ToString());

        public static Triangle[] CreateTriangles(Point[] points)
        {
            int numOfPts = points.Length;

            int numOfTriangles = ((numOfPts) * (numOfPts - 1) * (numOfPts - 2)) / 6;

            Triangle[] triangles = new Triangle[numOfTriangles];
            int trianglesCounter = 0;

            for (int i = 0; i < numOfPts; i++)
            {
                for (int j = i + 1; j < numOfPts; j++)
                {
                    for (int k = j + 1; k < numOfPts; k++)
                    {
                        triangles[trianglesCounter] = new Triangle(points[i], points[j], points[k]);
                        trianglesCounter++;
                    }
                }
            }

            return triangles;

        }

        public static double CalculateMostProbableOrientation(
            Dictionary<double, Tuple<Triangle, Triangle>> orientDiffToTrigs)
        {

            //divide into 20 buckets and find the most "crowded" bucket
            int bucketsSize = (int) (orientDiffToTrigs.Keys.Max() - orientDiffToTrigs.Keys.Min()) / 20;

            //bucket value to an array of orientations
            Dictionary<int, List<double>> buckets = new Dictionary<int, List<double>>(20);
            double[] orientations = orientDiffToTrigs.Keys.ToArray();

            for (int i = 0; i < orientations.Length; i++)
            {
                double currentOrientation = orientations[i];
                int bucketNum = (int) (Math.Abs(currentOrientation)/bucketsSize);

                if (!buckets.ContainsKey(bucketNum))
                {
                    buckets.Add(bucketNum, new List<double>());
                }

                buckets[bucketNum].Add(currentOrientation);
            }

            //only now we can check and remove all the orientation diffs of the none one-to-one triangle tuples
            //iterate threw the buckets and remove all the none one-to-ones
            int[] bucketValues = buckets.Keys.ToArray();

            List<double> mostCrowded = new List<double>();
            List<double> currentBucket;
            double currentOrient;
            double iterativeOrient;

            for (int i = 0; i < bucketValues.Length; i++)
            {
               currentBucket = buckets[i];

                for (int j = 0; j < currentBucket.Count; j++)
                {
                    currentOrient = currentBucket[j];
                    for (int k = 0; k < currentBucket.Count; k++)
                    {
                        iterativeOrient = currentBucket[k];
                        if (currentOrient != iterativeOrient &&
                            (orientDiffToTrigs[currentOrient].Item1 == orientDiffToTrigs[iterativeOrient].Item1 ||
                             orientDiffToTrigs[currentOrient].Item1 == orientDiffToTrigs[iterativeOrient].Item2 ||
                             orientDiffToTrigs[currentOrient].Item2 == orientDiffToTrigs[iterativeOrient].Item1 ||
                             orientDiffToTrigs[currentOrient].Item2 == orientDiffToTrigs[iterativeOrient].Item2))
                        {
                            currentBucket.Remove(currentOrient);
                        }
                        
                    }
                }

                //we already iterate threw all the items, lets find the most crowded
                if (mostCrowded.Count < currentBucket.Count)
                {
                    mostCrowded = currentBucket;
                }
                
            }

            //return the median difference as the most probable orientation 
            //TODO: naive algorithm - O(nlogn) can be implemented in O(n)
            mostCrowded.Sort();
            return mostCrowded[mostCrowded.Count/2];

        }

        public static Dictionary<double, Tuple<Triangle, Triangle>> FilterAndCalculateOrientationDiff(Triangle[] triangles1,
                                                                                       Triangle[] triangles2)
        {
            int numOfTriangles1 = triangles1.Length;
            int numOfTriangles2 = triangles2.Length;
            double angleMinDiff = Double.Parse(Settings.Default.Properties["anglesThreshold"].DefaultValue.ToString());

            Triangle currentTriangle1;
            Triangle currentTriangle2;

            Dictionary<double, Tuple<Triangle, Triangle>> qualifiedPairs = new Dictionary<double, Tuple<Triangle, Triangle>>();

            for (int i = 0; i < numOfTriangles1; i++)
            {
                currentTriangle1 = triangles1[i];

                for (int j = 0; j < numOfTriangles2; j++)
                {
                    currentTriangle2 = triangles2[j];

                    if ((Math.Abs(currentTriangle1.Angles[0] - currentTriangle2.Angles[0]) < angleMinDiff) &&
                        (Math.Abs(currentTriangle1.Angles[1] - currentTriangle2.Angles[1]) < angleMinDiff) &&
                        (Math.Abs(currentTriangle1.Angles[2] - currentTriangle2.Angles[2]) < angleMinDiff))
                    {
                        double orientationDiff = currentTriangle1.OrientationAverage -
                                                 currentTriangle2.OrientationAverage;

                        //filter out orientations of dumb triangles - 3 points on the same line..
                        if (currentTriangle1.IsLine || currentTriangle2.IsLine)
                        {
                            continue;
                        }

                        try
                        {
                            qualifiedPairs.Add(orientationDiff,
                                                  new Tuple<Triangle, Triangle>(currentTriangle1, currentTriangle2));
                        }
                        catch (ArgumentException)
                        {

                            Tuple<Triangle, Triangle> existingTuple = qualifiedPairs[orientationDiff];
                            if (
                                !((existingTuple.Item1.Equals(currentTriangle1) &&
                                   existingTuple.Item2.Equals(currentTriangle2))) ||
                                (existingTuple.Item1.Equals(currentTriangle2) &&
                                 existingTuple.Item2.Equals(currentTriangle1)))
                            {
                                //a pair of triangles with the same orientation diff already exist but its not the same triangles. add epsilon
                                bool addOrientation = true;
                                int numOfTries = 1;
                                while (qualifiedPairs.ContainsKey(orientationDiff + numOfTries * _epsilon))
                                {
                                    numOfTries++;
                                    if (numOfTries == 20)
                                    {
                                        addOrientation = false;
                                    }
                                }

                                if (addOrientation)
                                {
                                    qualifiedPairs.Add(orientationDiff + numOfTries * _epsilon,
                                                      new Tuple<Triangle, Triangle>(currentTriangle1, currentTriangle2));
                                }
                                
                            }
                            
                        }
                        



                    }
                }
            }

            return qualifiedPairs;
        }

    }
}
