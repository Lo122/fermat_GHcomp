using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace fermatspiral
{
    public class fermatspiralInfo : GH_AssemblyInfo
    {
        public override string Name => "fermatspiral";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("704360b8-c8b8-4c61-8c31-55ef8049d867");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}