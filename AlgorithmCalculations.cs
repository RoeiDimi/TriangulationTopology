﻿using System;
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
                OrderClockWise(currentTriangle1);

                for (int j = 0; j < numOfTriangles2; j++)
                {
                    currentTriangle2 = triangles2[j];
                    OrderClockWise(currentTriangle2);

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
                            //maybe a pair of triangles with the same orientation diff already exist but its not the same triangles. add epsilon
                            AddWithEpsilon(qualifiedPairs, qualifiedPairs[orientationDiff], currentTriangle1, currentTriangle2, orientationDiff);                            

                        }

                    }
                }
            }

            return qualifiedPairs;
        }

        public static double CalculateMostProbableOrientation(Dictionary<double, Tuple<Triangle, Triangle>> orientDiffToTrigs)
        {
            Dictionary<int, List<double>> buckets = CreateOrientationsBuckets(orientDiffToTrigs.Keys.ToArray());

            //only now we can check and remove all the orientation diffs of the none one-to-one triangle tuples
            List<double> mostCrowded = GetMostCrowdedOneToOne(buckets, orientDiffToTrigs);

            //return the median difference as the most probable orientation 
            //TODO: naive algorithm - O(nlogn) can be implemented in O(n)
            mostCrowded.Sort();
            return mostCrowded[mostCrowded.Count / 2];

        }

        private static void AddWithEpsilon(Dictionary<double, Tuple<Triangle, Triangle>> orientDiffToTrigs, 
                                Tuple<Triangle, Triangle> existingTuple, Triangle currentTriangle1, Triangle currentTriangle2, double orientationDiff)
        {
            if (!((existingTuple.Item1.Equals(currentTriangle1) && existingTuple.Item2.Equals(currentTriangle2))) ||
                (existingTuple.Item1.Equals(currentTriangle2) && existingTuple.Item2.Equals(currentTriangle1)))
            {
                //a pair of triangles with the same orientation diff already exist but its not the same triangles. add epsilon
                bool addOrientation = true;
                int numOfTries = 1;
                while (orientDiffToTrigs.ContainsKey(orientationDiff + numOfTries * _epsilon))
                {
                    numOfTries++;
                    if (numOfTries == int.Parse(Settings.Default.Properties["NumOfEpsilonInsertRetries"].DefaultValue.ToString()))
                    {
                        addOrientation = false;
                        break;
                    }
                }

                if (addOrientation)
                {
                    orientDiffToTrigs.Add(orientationDiff + numOfTries * _epsilon,
                                      new Tuple<Triangle, Triangle>(currentTriangle1, currentTriangle2));
                }

            }
        }
        private static bool IsClockwisePolygon(Point[] polygon)
        {
            bool isClockwise = false;
            double sum = 0;
            for (int i = 0; i < polygon.Length - 1; i++)
            {
                sum += (polygon[i + 1].X - polygon[i].X) * (polygon[i + 1].Y + polygon[i].Y);
            }

            sum += (polygon[0].X - polygon[polygon.Length - 1].X) * (polygon[0].Y + polygon[polygon.Length - 1].Y);

            isClockwise = (sum > 0) ? true : false;
            return isClockwise;
        }

        private static void OrderClockWise(Triangle triangle)
        {

            if (IsClockwisePolygon(triangle.Vertices))
            {
                //already ordered clockwise
                return;
            }

            //try other combinations. original order - 123

            //132
            SwapVertices(triangle, 1, 2);
            if (IsClockwisePolygon(triangle.Vertices))
            {
                return;
            }

            //312
            SwapVertices(triangle, 0, 1);
            if (IsClockwisePolygon(triangle.Vertices))
            {
                return;
            }

            //321
            SwapVertices(triangle, 1, 2);
            if (IsClockwisePolygon(triangle.Vertices))
            {
                return;
            }

            //231
            SwapVertices(triangle, 0, 1);
            if (IsClockwisePolygon(triangle.Vertices))
            {
                return;
            }

            //213
            SwapVertices(triangle, 1, 2);
            if (IsClockwisePolygon(triangle.Vertices))
            {
                return;
            }

            throw new Exception("Error! no possible clockwise order was found for a triangle");

        }

        private static void SwapVertices(Triangle triangle, int index1, int index2)
        {

            Point temp = triangle.Vertices[index1];
            triangle.Vertices[index1] = triangle.Vertices[index2];
            triangle.Vertices[index2] = temp;

        }

        private static Dictionary<int, List<double>> CreateOrientationsBuckets(double[] orientations)
        {

            
            //divide into 20 buckets and find the most "crowded" bucket
            int bucketsSize = (int)(orientations.Max() - orientations.Min()) / 
                            int.Parse(Settings.Default.Properties["NumOfBuckets"].DefaultValue.ToString());

            //bucket value to an array of orientations
            Dictionary<int, List<double>> buckets = new Dictionary<int, List<double>>(20);

            for (int i = 0; i < orientations.Length; i++)
            {
                double currentOrientation = orientations[i];
                int bucketNum = (int)(Math.Abs(currentOrientation) / bucketsSize);

                if (!buckets.ContainsKey(bucketNum))
                {
                    buckets.Add(bucketNum, new List<double>());
                }

                buckets[bucketNum].Add(currentOrientation);
            }

            return buckets;

        }

        private static List<double> GetMostCrowdedOneToOne(Dictionary<int, List<double>> buckets, 
                                                                            Dictionary<double, Tuple<Triangle, Triangle>> orientDiffToTrigs)
        {

            //iterate through the buckets, remove all the none one-to-ones and find the most crowded
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

            return mostCrowded;
        }

       

    }
}
