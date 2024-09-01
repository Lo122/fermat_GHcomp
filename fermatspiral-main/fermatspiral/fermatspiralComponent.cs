using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Geometry.Delaunay;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Render.ChangeQueue;
using Plane = Rhino.Geometry.Plane;

namespace fermatspiral
{
    public class fermatspiralComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public fermatspiralComponent()
          : base("fermatspiral", "fermat",
            "Description",
            "Curve", "Util")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Offset Curve","oc", "set offset curves in {a;b} format", GH_ParamAccess.tree);
            pManager.AddPointParameter("Attractive Point","ap","set attractive point",GH_ParamAccess.list);
            pManager.AddNumberParameter("Offset Dis","od","set offset distance",GH_ParamAccess.item);
            pManager.AddNumberParameter("Divide Len","dl","set divide length",GH_ParamAccess.item);

            pManager[0].Optional = false;
            pManager[1].Optional = false;
            pManager[2].Optional = false;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve Group", "cg", "grouped curve based on inclusion relationship", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Fermat Curve", "fc", "fermat curve", GH_ParamAccess.list);

            pManager.AddPointParameter("Centroids", "c", "centrods", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        [Obsolete]
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Grasshopper.DataTree<Curve> curveTree = new DataTree<Curve> ();
            GH_Structure<GH_Curve> curveList = new GH_Structure<GH_Curve>();
            GH_Structure<GH_Curve> curveGroup = new GH_Structure<GH_Curve>();
            List<Curve> curveFermat = new List<Curve>();

            List<Point3d> cenPt = new List<Point3d>();
            Dictionary<int, List<Curve>>curveDic = new Dictionary<int, List<Curve>>();
            Dictionary<int, List< Curve>> fCrvDic = new Dictionary<int,List<Curve>>();
            Dictionary<int, List<Curve>> fDic = new Dictionary<int, List<Curve>>();
            Dictionary<int,List<int>>curveLenDic = new  Dictionary<int, List<int>>();

            //Point3d attraPoint = new Point3d (0, 0, 0);
            List<Point3d>attraPt = new List<Point3d>(); 

            GH_Structure<GH_Curve> sortGroup = new GH_Structure<GH_Curve>();

            double offsetDis = 5;
            double divLen = 5;

            DA.GetDataTree(0, out curveList);
            DA.GetDataList(1,  attraPt);

            DA.GetData(2, ref offsetDis);
            DA.GetData(3, ref divLen);

            //Plane plane = new Plane(attraPoint, Vector3d.ZAxis);
            
            int patha =0;
            int pathb = 0;
            fCrvDic.Add(0,new List<Curve>());
            fDic.Add(0, new List<Curve>());
            curveDic.Add(0, new List<Curve>());
            curveLenDic.Add (0, new List<int>());
            int branchCount = 1;

            #region sort groups
            for (int i = 0; i < curveList.PathCount-1; i++)
            {
                var pathA = curveList.Paths[i][0];
                var path = new GH_Path(pathA, pathb);
                var curveToAdd = curveList[curveList.Paths[i]];
                foreach (var item in curveToAdd)
                  {
                    curveDic[pathA].Add(item.Value);
                    curveLenDic[pathA].Add((int)item.Value.GetLength());
                    var check = item.Value.GetLength();
                    var newItem = new GH_Curve( Rebuild(item.Value, divLen));

                    if (newItem.Value.GetLength() > divLen)
                    {
                        curveGroup.Append(newItem, path);
                    }

                  }
                if (curveList[curveList.Paths[i + 1]].Count != branchCount)
                {
                    pathb++;
                    branchCount = curveList[curveList.Paths[i + 1]].Count;
                }
                if (curveList.Paths[i + 1][0] != patha)
                {
                    pathb = 0;
                    patha++;
                    fCrvDic.Add(patha,new List<Curve>());
                    fDic.Add(patha, new List<Curve>());
                    curveDic.Add(patha, new List<Curve>());
                    curveLenDic.Add(patha, new List<int>());
                    branchCount = 1;
                }
            }

            // add curves in the last path:
            var lastPath = curveList.Paths.Last();
            var lastCurve = curveList[lastPath];

            foreach(var cItem in lastCurve)
            {
                var newItem = new GH_Curve(Rebuild(cItem.Value, divLen));
                if (cItem.Value.GetLength() > divLen)
                {
                    curveGroup.Append(newItem, curveGroup.Paths.Last());
                }
            }
            #endregion


                     
            for (int j = 0; j < curveGroup.PathCount; j++)
            {
                GH_Path fPath = new GH_Path(curveGroup.Paths[j][0],0);
                List<Curve> cList = new List<Curve>();
                Point3d attraPoint = new Point3d(0, 0, 0);
                if (curveGroup.Paths[j][1] == 0)
                {
                    foreach(var item in curveGroup[curveGroup.Paths[j]])
                    {
                        
                        var newItem = Rebuild(item.Value,divLen);
                        cList.Add(newItem);
                        var see = newItem.GetLength();
                        sortGroup.Append(new GH_Curve(newItem),fPath);
                    }
                    if(attraPt.Count <= curveGroup.Paths[j][0])
                    {
                        attraPoint = attraPt.Last();
                    }
                    else
                    {
                        attraPoint = attraPt[curveGroup.Paths[j][0]];
                    }

                    var ferC = ConstructFermat(cList, attraPoint, offsetDis,divLen);
                    if (ferC != null)
                    {
                        curveFermat.Add(ferC);
                        fDic[curveGroup.Paths[j][0]].Add(ferC);
                    }
                }
                else
                {
                    foreach (var item in curveGroup[curveGroup.Paths[j]])
                    {
                        fCrvDic[curveGroup.Paths[j][0]].Add(item.Value);
                    }
                }
            }

            
            for(int j = 0; j<fCrvDic.Count; j++)
            {
                var crvToSort = fCrvDic[j];
                List<int> lenL = new List<int>();
                //var item = crvToSort[j];

                    #region // find inclusion relationships
                    List<Curve> cenCrv = new List<Curve>();
                for(int k = 0; k < crvToSort.Count; k++)
                {
                    var item = crvToSort[k];
                    bool s = InsideCrv(item, curveDic[j][0], offsetDis);
                        if (s)
                        {
                            if (item.GetLength() < offsetDis)
                            {
                                cenCrv.Add(item);
                            }
                            else
                            {
                                var b = Brep.CreatePlanarBreps(item);

                                AreaMassProperties amp = AreaMassProperties.Compute(b);
                                Point3d centP = amp.Centroid;

                                var result = item.Offset(centP, Vector3d.ZAxis, offsetDis, 0, CurveOffsetCornerStyle.Smooth);
                                var result1 = item.Offset(centP, Vector3d.ZAxis, -offsetDis, 0, CurveOffsetCornerStyle.Smooth);

                                if (result == null || result1 == null)
                                {
                                    cenCrv.Add(item);
                                }
                                else
                                {
                                    List<Curve> resL = new List<Curve>();
                                    var res = Curve.JoinCurves(result);
                                    var res1 = Curve.JoinCurves(result1);
                                    resL.Add(res[0]);
                                    resL.Add(res1[0]);

                                    resL.OrderByDescending(x => x.GetLength());

                                    var len = res[0].GetLength();
                                    var len1 = res1[0].GetLength();
                                int L = (int)resL.Last().GetLength();

                                bool dir = Curve.DoDirectionsMatch(res.Last(),item);
                                //bool dir = curveLenDic[j].Contains(L);
                                    if ( L < offsetDis||dir == false)
                                    //if (dot<0)
                                    {
                                        cenCrv.Add(item);
                                    }


                                }
                            }

                        }

                }

                var see = cenCrv;
                //DA.SetDataList(2,cenCrv);

                #endregion

                for (int q = crvToSort.Count-1; q >=0; q--)
                {
                    if (cenCrv.Contains(crvToSort[q]))
                    {
                        crvToSort.RemoveAt(q);
                    }
                }

                var cts = crvToSort;
                #region

                int index = 1;

              foreach (var crv in cenCrv)
                {
                    List<Curve> lastCrv = new List<Curve>();
                    List<Curve> crvList = new List<Curve>();

                    var b = Brep.CreatePlanarBreps(crv);
                    AreaMassProperties amp = AreaMassProperties.Compute(b);
                    Point3d centP = amp.Centroid;

                    cenPt.Add(centP);

                    GH_Path fPath = new GH_Path(j, index);
                    sortGroup.Append(new GH_Curve(crv), fPath);

                    crvList.Add(crv);

                    for (int p = cts.Count - 1; p >= 0; p--)
                    {
                        var outC = cts[p];

                            if (InsideCrv(crv, outC, offsetDis))
                            {
                                crvList.Add(outC);

                                cts.RemoveAt(p);

                                sortGroup.Append(new GH_Curve(outC), fPath);
                            }

                    }

                    index++;

                    // cenCrv = lastCrv;
                    crvList.Reverse();
                    var cl = new List<Curve>();
                    foreach (var cc in crvList)
                    {
                        if (cc.GetLength() >= offsetDis * 2)
                        {
                            cl.Add(cc); 
                        }
                    }
                    var ferC = ConstructFermat(cl, centP, offsetDis, divLen);
                    fDic[j].Add(ferC);
                    curveFermat.Add(ferC);
                  

                }
              
                #endregion 

            }

            DA.SetDataTree(0, sortGroup);

            List<Curve> finalCrv = new List<Curve>();//convert fDic eventually

            for (int i = 0; i < fDic.Count; i++)
            {
                foreach(var c in fDic[i])
                {
                    finalCrv.Add(c);
                }
            }
            
            DA.SetDataList(1, finalCrv);

            DA.SetDataList(2, cenPt);
        }
        public Curve c;
        public Curve ConstructFermat(List<Curve> curveList,Point3d pointList, double offsetDis, double divideLen)
        {
            double t = 0;
            double l = 0;

            List < Curve > curveL = new List<Curve> ();
            List<Point3d> pluPt = new List<Point3d>();
            List<Point3d> sinPt = new List<Point3d>();

            List<Point3d>collPt = new List<Point3d>();  

            for (int i = 0; i < curveList.Count; i++)
            {
                Curve curve = curveList[i];

                bool flag = curve.ClosestPoint(pointList, out t);
                if (flag)
                {
                    Point3d sPoint = curve.PointAt(t);
                    curve.ChangeClosedCurveSeam(t);
                    if (!Curve.DoDirectionsMatch(curve, curveList.Last()))
                    {
                        curve.Reverse();
                    }
                    Point3d ePoint = curve.PointAtLength(offsetDis);

                    if(i == curveList.Count - 1)
                    {
                        curve.LengthParameter(offsetDis , out l);
                    }
                    
                    else
                    {
                        curve.LengthParameter(offsetDis * 2, out l);
                    }

                    c = curve.Trim(l, t);

                    if (c != null )
                    {
                        curveL.Add(c);
                    }

                }
            }

            
            for (int i =0; i < curveL.Count; i++)
            {
                //var see = curveL[i].GetLength();
                Point3d[] collP;
                if( i % 2 == 0)
                {
                    var d = curveL[i].GetLength() / divideLen;
                    int dCount = (int)Math.Round(d)+1;
                    //curveL[i].Rebuild(dCount, 1, true);

                    curveL[i].DivideByCount(dCount,true,out collP); 
                    //curveL[i].DivideByLength(divideLen, true, out collP);

                    if (collP != null)
                    {
                        pluPt.AddRange(collP);
                    }
                    
                }
                if(i % 2 == 1)
                {
                    var d = curveL[i].GetLength() / divideLen;
                    int dCount = (int)Math.Round(d) + 1;
                    curveL[i].Rebuild(dCount, 1, true);

                    curveL[i].DivideByCount(dCount, true, out collP);

                    if (collP != null)
                    {
                        sinPt.AddRange(collP);
                    }
                }
            }
            sinPt.Reverse();

            collPt.AddRange(pluPt);
            collPt.AddRange(sinPt); 
            if(collPt.Count > 0)
            {
                Curve ferCrv = Curve.CreateInterpolatedCurve(collPt, 1);
                return ferCrv;
            }
            else
            {
                return null;
            }

        }
        
        public bool InsideCrv(Curve cenCrv, Curve outCrv, double divLen)
        {
            //0=disjoitn, 1=mutualintersection, 2=AinB, 3=BinA
            var res = Curve.PlanarClosedCurveRelationship(cenCrv, outCrv, Plane.WorldXY, 1);
            if(res == RegionContainment.AInsideB)
            {
                return true;
            }
            else { return false; }
        }

        public Curve Rebuild(Curve curve, double divLen)
        {
            List<Point3d> collPt = new List<Point3d>();
            var cl = curve.DuplicateSegments();

            if (curve.ToString() != "Rhino.Geometry.PolylineCurve")
            {
                var ptToAdd = curve.DivideEquidistant(divLen);
                collPt.AddRange(ptToAdd);
            }
            else
            {
                foreach (var item in cl)
                {
                    collPt.Add(item.PointAtStart);
                }

            }

            collPt.Add(collPt[0]);
            Curve res = Curve.CreateInterpolatedCurve(collPt.ToArray(),1);
            
            
            return res;
        }
        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        { get { return fermatspiral.Properties.Resources.fermat; } }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("ecdbbfaa-b99d-43cb-bae9-91e7df1c5cf2");
    }
}