using System.Collections.Generic;
using UnityEngine;
using Dim = Freedome.Environment.ShedDimensions;
using Keys = Freedome.EditorTools.Generation.ShedMaterialLibrary.Keys;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// The pitched roof: rafters, ridge board, collar ties, purlins, corrugated
    /// steel, fascia and barge boards, and the eaves blocking between rafters.
    ///
    /// The roof is built with purlins and a bare corrugated sheet rather than a
    /// lined ceiling, because that is how a shed of this size is normally put
    /// together and because it means the player looks up at real structure. It also
    /// produces the one honest daylight leak in the building: the corrugation
    /// profile does not sit flat on the eaves blocking, so a row of thin crescents
    /// of sky light runs along the top of each long wall.
    /// </summary>
    public static class RoofBuilder
    {
        public const float PurlinThickness = 0.045f; // perpendicular to the roof plane
        public const float PurlinWidth = 0.070f;     // measured along the slope
        public const float CorrugationPitch = 0.076f;
        public const float CorrugationAmplitude = 0.016f;

        /// <summary>Ridge cap wing, measured along the slope.</summary>
        public const float CapWidth = 0.300f;

        /// <summary>
        /// Distance down the slope from the ridge board to the centre of the cap
        /// wing. Must be small enough that the wing's inner edge crosses the
        /// centreline; <c>RidgeCapClosesTheApex</c> is what keeps it honest.
        /// </summary>
        public const float CapCentreOffset = 0.130f;

        /// <summary>
        /// Signed x of the cap wing's inner edge, for the +x side. Negative means it
        /// has crossed the apex and the two wings overlap, which is what closing the
        /// ridge requires. Shared with the tests rather than recomputed there.
        /// </summary>
        public static float RidgeCapInnerEdgeX =>
            RidgeOffset + ((CapCentreOffset - (CapWidth * 0.5f)) * CosTheta);
        public const float BargeThickness = 0.019f;
        public const float FasciaHeight = 0.140f;

        private static float Theta => Dim.RoofPitchDegrees * Mathf.Deg2Rad;

        private static float CosTheta => Mathf.Cos(Theta);

        /// <summary>Half the roof length including the gable overhang.</summary>
        public static float RoofHalfLength => Dim.HalfLength + Dim.WallThickness + Dim.GableOverhang;

        /// <summary>Outer X of the eave, past the wall face by the eave overhang.</summary>
        public static float EaveX => Dim.RoofHalfSpan + Dim.EaveOverhang;

        /// <summary>Half the ridge board thickness - where the rafters butt.</summary>
        private static float RidgeOffset => Dim.RidgeBoardThickness * 0.5f;

        /// <summary>Length of one rafter measured along the slope.</summary>
        public static float RafterLength => (EaveX - RidgeOffset) / CosTheta;

        /// <summary>Underside of the rafter line at a given X.</summary>
        private static float UndersideY(float x) => Dim.RoofUndersideAt(Mathf.Abs(x));

        /// <summary>
        /// Basis for anything lying in the roof plane. Local X runs along the slope,
        /// local Y is perpendicular to it, local Z runs along the ridge.
        /// </summary>
        private static Quaternion SlopeRotation(int side)
        {
            Vector3 up = new Vector3(side * Mathf.Sin(Theta), CosTheta, 0f);
            return Quaternion.LookRotation(Vector3.forward, up);
        }

        private static Vector3 SlopeNormal(int side) =>
            new Vector3(side * Mathf.Sin(Theta), CosTheta, 0f);

        /// <summary>Rafter Z positions, including the two barge rafters past the gables.</summary>
        public static List<float> RafterPositions()
        {
            List<float> list = new List<float>();
            for (float z = -Dim.HalfLength; z <= Dim.HalfLength + 0.001f; z += Dim.RafterSpacing)
            {
                list.Add(Mathf.Round(z * 1000f) / 1000f);
            }
            list.Insert(0, -(RoofHalfLength - 0.075f));
            list.Add(RoofHalfLength - 0.075f);
            return list;
        }

        public static void Build(BuildContext ctx, Transform parent)
        {
            BuildStructure(ctx, parent);
            BuildCovering(ctx, parent);
            BuildEavesBlocking(ctx, parent);
        }

        // =====================================================================
        // Structure: rafters, ridge, collars, purlins, fascia and barges
        // =====================================================================

        private static void BuildStructure(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Shed_RoofStructure", 1);
            const float Bevel = 0.0025f;

            List<float> rafterZ = RafterPositions();

            // --- ridge board -------------------------------------------------
            float ridgeCentreY = Dim.RidgeHeight - (Dim.RidgeBoardHeight * 0.35f);
            mb.AddBox(new Vector3(0f, ridgeCentreY, 0f),
                      new Vector3(Dim.RidgeBoardThickness, Dim.RidgeBoardHeight, RoofHalfLength * 2f),
                      0, Bevel);

            // --- rafters -------------------------------------------------------
            float midX = (RidgeOffset + EaveX) * 0.5f;
            float midY = UndersideY(midX);
            float length = RafterLength;

            foreach (float z in rafterZ)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 n = SlopeNormal(side);
                    Vector3 centre = new Vector3(side * midX, midY, z) + (n * (Dim.RafterDepth * 0.5f));
                    mb.AddBox(centre,
                              new Vector3(length, Dim.RafterDepth, Dim.RafterWidth),
                              SlopeRotation(side), 0, Bevel);
                }
            }

            // --- collar ties, every third rafter pair ---------------------------
            float collarUnder = Dim.CollarTieHeight;
            float collarHalfSpan = Dim.RoofHalfSpan -
                                   ((collarUnder - Dim.WallHeight) / Mathf.Tan(Theta));
            collarHalfSpan += 0.09f; // lap onto the face of each rafter

            for (float z = -2.4f; z <= 2.4f + 0.001f; z += 1.2f)
            {
                float zz = z + (Dim.RafterWidth * 0.5f) + (Dim.CollarTieThickness * 0.5f);
                mb.AddBox(new Vector3(0f, collarUnder + 0.045f, zz),
                          new Vector3(collarHalfSpan * 2f, 0.090f, Dim.CollarTieThickness), 0, Bevel);
            }

            // --- purlins, carrying the sheeting ---------------------------------
            float[] purlinAlongSlope = { 0.30f, 1.10f, 1.90f, 2.45f };
            foreach (float t in purlinAlongSlope)
            {
                if (t > length)
                {
                    continue;
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 n = SlopeNormal(side);
                    Vector3 alongSlope = new Vector3(side * CosTheta, -Mathf.Sin(Theta), 0f);
                    Vector3 ridgeStart = new Vector3(side * RidgeOffset, UndersideY(RidgeOffset), 0f);
                    Vector3 onLine = ridgeStart + (alongSlope * t);
                    Vector3 centre = onLine + (n * (Dim.RafterDepth + (PurlinThickness * 0.5f)));

                    mb.AddBox(centre,
                              new Vector3(PurlinWidth, PurlinThickness, RoofHalfLength * 2f),
                              SlopeRotation(side), 0, Bevel);
                }
            }

            // --- fascia at the eaves --------------------------------------------
            float eaveUnder = UndersideY(EaveX);
            for (int side = -1; side <= 1; side += 2)
            {
                mb.AddBox(new Vector3(side * (EaveX + (BargeThickness * 0.5f)),
                                      eaveUnder + (FasciaHeight * 0.5f) - 0.02f, 0f),
                          new Vector3(BargeThickness, FasciaHeight, RoofHalfLength * 2f), 0, 0.003f);
            }

            // --- barge boards on the gable ends ---------------------------------
            foreach (int endSign in new[] { -1, 1 })
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 n = SlopeNormal(side);
                    Vector3 centre = new Vector3(side * midX, midY, endSign * (RoofHalfLength - (BargeThickness * 0.5f)))
                                     + (n * (Dim.RafterDepth + (PurlinThickness * 0.5f)));
                    mb.AddBox(centre,
                              new Vector3(length, FasciaHeight, BargeThickness),
                              SlopeRotation(side), 0, 0.003f);
                }
            }

            ctx.CreateObject("Shed_RoofStructure", mb, new[] { Keys.StructuralPine },
                parent, Vector3.zero, Quaternion.identity, BuildContext.ColliderKind.Mesh);
        }

        // =====================================================================
        // Covering: corrugated sheet plus a folded ridge cap
        // =====================================================================

        private static void BuildCovering(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Shed_RoofSheeting", 1);
            mb.UvScale = 1f;

            float length = RafterLength;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 n = SlopeNormal(side);
                Vector3 ridgeStart = new Vector3(side * RidgeOffset, UndersideY(RidgeOffset), 0f);
                Vector3 origin = ridgeStart
                                 + (n * (Dim.RafterDepth + PurlinThickness))
                                 + new Vector3(0f, 0f, -RoofHalfLength);

                mb.AddCorrugatedSheet(origin, SlopeRotation(side), length, RoofHalfLength * 2f,
                                      CorrugationPitch, CorrugationAmplitude, 0);
            }

            // Ridge capping: two wings, one lying in each slope plane, meeting over
            // the apex the way a folded cap does.
            //
            // The inner edge has to cross x = 0, not merely reach the ridge board.
            // The sheets stop at ridgeStart, which is half the ridge board's
            // thickness out from the centreline, so the cap is the only thing
            // closing that slot. At the previous 0.14 m the wings stopped 3.2 mm
            // short of the centreline each, leaving a 6.5 mm gap straight through
            // the roof for the whole 6.9 m of the ridge - a hard line of daylight
            // down the apex. CapCentreOffset is what closes it, with 12 mm of
            // overlap so the two wings interpenetrate slightly at the fold.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 n = SlopeNormal(side);
                Vector3 alongSlope = new Vector3(side * CosTheta, -Mathf.Sin(Theta), 0f);
                Vector3 ridgeStart = new Vector3(side * RidgeOffset, UndersideY(RidgeOffset), 0f);
                Vector3 centre = ridgeStart + (alongSlope * CapCentreOffset)
                                 + (n * (Dim.RafterDepth + PurlinThickness + CorrugationAmplitude + 0.004f));

                mb.AddBox(centre, new Vector3(CapWidth, 0.006f, RoofHalfLength * 2f),
                          SlopeRotation(side), 0, 0.002f);
            }

            ctx.CreateObject("Shed_RoofSheeting", mb, new[] { Keys.Galvanised },
                parent, Vector3.zero, Quaternion.identity, BuildContext.ColliderKind.Mesh);
        }

        // =====================================================================
        // Eaves blocking
        // =====================================================================

        /// <summary>
        /// A tapered block of timber in each rafter bay, sitting on the top plate and
        /// running up to the underside of the corrugation troughs. It closes the eave
        /// against weather and vermin the way a real one does, while leaving the
        /// crest of every corrugation open - which is exactly where the thin lines of
        /// daylight along the top of the walls come from.
        /// </summary>
        private static void BuildEavesBlocking(BuildContext ctx, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder("Shed_EavesBlocking", 1);

            float xInner = Dim.HalfWidth;                 // 2.00, inner face of the frame
            float xOuter = Dim.HalfWidth + Dim.StudDepth; // 2.09, outer face of the frame

            float hInner = TroughY(xInner) - Dim.WallHeight;
            float hOuter = TroughY(xOuter) - Dim.WallHeight;

            Vector2[] profile =
            {
                new Vector2(0f, 0f),
                new Vector2(Dim.StudDepth, 0f),
                new Vector2(Dim.StudDepth, hOuter),
                new Vector2(0f, hInner),
            };

            List<float> rafterZ = RafterPositions();
            rafterZ.Sort();

            for (int i = 0; i < rafterZ.Count - 1; i++)
            {
                float z0 = rafterZ[i] + (Dim.RafterWidth * 0.5f);
                float z1 = rafterZ[i + 1] - (Dim.RafterWidth * 0.5f);

                // Only block the bays that sit over the walls.
                if (z1 <= -Dim.HalfLength || z0 >= Dim.HalfLength)
                {
                    continue;
                }

                z0 = Mathf.Max(z0, -Dim.HalfLength - Dim.StudDepth);
                z1 = Mathf.Min(z1, Dim.HalfLength + Dim.StudDepth);
                float bay = z1 - z0;
                if (bay < 0.05f)
                {
                    continue;
                }

                // +X wall: profile runs outward along +X, extruded along +Z.
                mb.AddExtrusion(profile, bay, new Vector3(xInner, Dim.WallHeight, z0),
                                Quaternion.identity, 0);

                // -X wall: mirrored, so extrude the other way along Z.
                mb.AddExtrusion(profile, bay, new Vector3(-xInner, Dim.WallHeight, z1),
                                Quaternion.Euler(0f, 180f, 0f), 0);
            }

            ctx.CreateObject("Shed_EavesBlocking", mb, new[] { Keys.StructuralPine },
                parent, Vector3.zero, Quaternion.identity, BuildContext.ColliderKind.Mesh);
        }

        /// <summary>Height of the bottom of the corrugation troughs at a given X.</summary>
        public static float TroughY(float x)
        {
            float centreLine = UndersideY(x) + ((Dim.RafterDepth + PurlinThickness) / CosTheta);
            return centreLine - (CorrugationAmplitude * 0.5f);
        }
    }
}
