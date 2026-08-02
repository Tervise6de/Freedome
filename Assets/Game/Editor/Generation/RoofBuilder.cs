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

        /// <summary>
        /// Purlin centres, measured down the slope from the ridge. Shared with the
        /// sheet fixings, which have to land on a purlin to be holding anything.
        /// </summary>
        public static readonly float[] PurlinAlongSlope = { 0.30f, 1.10f, 1.90f, 2.45f };

        /// <summary>Thickness of the folded ridge cap.</summary>
        public const float CapThickness = 0.006f;

        /// <summary>
        /// How far past the ridge centreline each cap wing reaches, measured
        /// horizontally. This is the number that decides whether the roof is shut,
        /// so it is the number the cap is built from - width and offset are worked
        /// out from it rather than the other way round.
        /// </summary>
        public const float CapOverlapPastApex = 0.020f;

        /// <summary>How far down the slope the cap laps over the sheeting.</summary>
        public const float CapOuterEdgeAlongSlope = 0.280f;

        /// <summary>
        /// How far the cap is lifted off the rafter line: past the rafter, past the
        /// purlin, and clear of the corrugation crests it sits on.
        /// </summary>
        public static float CapLift =>
            Dim.RafterDepth + PurlinThickness + CorrugationAmplitude + 0.004f;

        /// <summary>
        /// Where the wing's own plane crosses x = 0, as a distance along the slope
        /// from <c>ridgeStart</c>. Negative, because the apex is up-slope of where
        /// the rafters butt the ridge board.
        ///
        /// The lift matters here and is easy to drop. Offsetting the cap along the
        /// slope normal moves it horizontally as well as vertically - by
        /// sin(pitch) x lift, which at 22 degrees over 155 mm is 58 mm. A version of
        /// this file computed the inner edge without that term, reported it as
        /// 43 mm past the apex, and passed its own test while the real edge sat
        /// 15 mm short of the ridge board face. The result was a 2.5 mm slot down
        /// each side of the ridge board, the full 6.9 m of the building.
        /// </summary>
        private static float CapApexAlongSlope =>
            -(RidgeOffset + (Mathf.Sin(Theta) * CapLift)) / CosTheta;

        private static float CapInnerEdgeAlongSlope =>
            CapApexAlongSlope - (CapOverlapPastApex / CosTheta);

        /// <summary>Ridge cap wing, measured along the slope.</summary>
        public static float CapWidth => CapOuterEdgeAlongSlope - CapInnerEdgeAlongSlope;

        /// <summary>Distance down the slope from the ridge board to the wing's centre.</summary>
        public static float CapCentreOffset =>
            (CapOuterEdgeAlongSlope + CapInnerEdgeAlongSlope) * 0.5f;

        /// <summary>
        /// Signed x of the cap wing's inner edge, for the +x side, read off the box
        /// the builder actually places rather than derived a second time. Negative
        /// means the wing has crossed the apex, which is what closing the ridge
        /// requires.
        /// </summary>
        public static float RidgeCapInnerEdgeX =>
            (CapWingCentre(1) + (SlopeRotation(1) * new Vector3(-CapWidth * 0.5f, 0f, 0f))).x;

        /// <summary>
        /// How far either side of the apex the two wings actually overlap in height.
        /// The wings must reach comfortably past this, not merely past zero.
        /// </summary>
        public static float RidgeCapSealBandHalfWidth =>
            CapThickness / (2f * Mathf.Tan(Theta));
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

                // Two coach bolts through each lap. A collar tie is the one piece of
                // the roof the player can see the whole length of, and floating
                // against the rafter with nothing holding it is what makes a roof
                // read as modelled rather than built.
                foreach (int sx in new[] { -1, 1 })
                {
                    foreach (float inset in new[] { 0.035f, 0.085f })
                    {
                        float bx = sx * (collarHalfSpan - inset);
                        mb.AddCylinder(new Vector3(bx, collarUnder + 0.045f,
                                                   zz - (Dim.CollarTieThickness * 0.5f) - 0.004f),
                                       0.0085f, 0.0085f, 0.008f, 8, 0,
                                       Quaternion.Euler(90f, 0f, 0f));
                    }
                }
            }

            // Galvanised straps over each rafter, down onto the top plate. This is
            // the connection that stops a shed roof lifting, it lives exactly where
            // the eye goes at the eaves, and it was the one piece of ironmongery the
            // structure had none of.
            float plateTop = Dim.WallHeight;
            foreach (float z in rafterZ)
            {
                if (Mathf.Abs(z) > Dim.HalfLength + 0.001f)
                {
                    continue;
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * (Dim.RoofHalfSpan - 0.020f);
                    float overRafter = UndersideY(Mathf.Abs(x)) + Dim.RafterDepth + 0.004f;

                    // Up the face of the rafter and over its top edge.
                    mb.AddBox(new Vector3(x, (plateTop + overRafter) * 0.5f - 0.020f,
                                          z + (Dim.RafterWidth * 0.5f) + 0.003f),
                              new Vector3(0.030f, overRafter - plateTop + 0.040f, 0.0025f), 0, 0f);
                    mb.AddBox(new Vector3(x - (side * 0.026f), overRafter,
                                          z + (Dim.RafterWidth * 0.5f) + 0.003f),
                              new Vector3(0.075f, 0.0025f, 0.0025f),
                              SlopeRotation(side), 0, 0f);
                }
            }

            // --- purlins, carrying the sheeting ---------------------------------
            foreach (float t in PurlinAlongSlope)
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
            ctx.CreateObject("Shed_RoofSheeting", BuildCoveringMesh(), new[] { Keys.Galvanised },
                parent, Vector3.zero, Quaternion.identity, BuildContext.ColliderKind.Mesh);
        }

        /// <summary>Where the point of the roof is, on a given side, before lifting.</summary>
        private static Vector3 RidgeStart(int side) =>
            new Vector3(side * RidgeOffset, UndersideY(RidgeOffset), 0f);

        private static Vector3 AlongSlope(int side) =>
            new Vector3(side * CosTheta, -Mathf.Sin(Theta), 0f);

        /// <summary>
        /// The frame a sheet of roofing is laid in: local +Z runs down the slope,
        /// local +Y is the slope normal, and local +X - the axis
        /// <see cref="MeshBuilder.AddCorrugatedSheet"/> corrugates along - runs
        /// horizontally, so the ribs run down the slope and the troughs drain.
        ///
        /// Not <see cref="SlopeRotation"/>, which is built for boxes and is not
        /// mirrored between the two slopes: its local +X points down the +X slope
        /// but *up* the -X one. A box does not care, because a box is symmetric
        /// about its centre. A sheet laid from a corner does: the -X sheeting was
        /// laid from the ridge up and over, which put both sheets on the +X side of
        /// the building and left the whole storage-side slope open to the sky.
        /// </summary>
        private static Quaternion SheetRotation(int side) =>
            Quaternion.LookRotation(AlongSlope(side), SlopeNormal(side));

        /// <summary>
        /// Centre of one ridge cap wing. Its own function because the tests need the
        /// position the builder uses, not a second derivation of it.
        /// </summary>
        public static Vector3 CapWingCentre(int side) =>
            RidgeStart(side) + (AlongSlope(side) * CapCentreOffset) + (SlopeNormal(side) * CapLift);

        /// <summary>
        /// The sheeting, its fixings and the folded ridge cap, with no scene objects
        /// involved, so a test can build it and look through it.
        /// </summary>
        public static MeshBuilder BuildCoveringMesh()
        {
            MeshBuilder mb = new MeshBuilder("Shed_RoofSheeting", 1);
            mb.UvScale = 1f;

            float length = RafterLength;

            for (int side = -1; side <= 1; side += 2)
            {
                // Lifted by half the amplitude so the sheet *rests* on the purlin
                // at its troughs instead of being centred on it. Centred, the trough
                // line cut 8 mm into every purlin it crossed, and with the ribs
                // running down the slope that is every purlin on the building: the
                // timber showed through the roof as a dotted line from ridge to eave.
                Vector3 origin = RidgeStart(side)
                                 + (SlopeNormal(side) *
                                    (Dim.RafterDepth + PurlinThickness + (CorrugationAmplitude * 0.5f)))
                                 + new Vector3(0f, 0f, side * RoofHalfLength);

                mb.AddCorrugatedSheet(origin, SheetRotation(side), RoofHalfLength * 2f, length,
                                      CorrugationPitch, CorrugationAmplitude, 0);
            }

            // Ridge capping: two wings, one lying in each slope plane, meeting over
            // the apex the way a folded cap does. The sheets stop at the ridge board,
            // so the cap is the only thing closing that slot; CapOverlapPastApex is
            // how far each wing carries past the centreline to do it.
            for (int side = -1; side <= 1; side += 2)
            {
                mb.AddBox(CapWingCentre(side),
                          new Vector3(CapWidth, CapThickness, RoofHalfLength * 2f),
                          SlopeRotation(side), 0, 0.002f);
            }

            AddSheetFixings(mb);
            return mb;
        }

        /// <summary>
        /// Screws with sealing washers through the crest of every third corrugation,
        /// on the line of each purlin. Sheet steel is fixed through the crest, not
        /// the trough, so the fixing sits above standing water - which is also why
        /// they read as a row of small bright dots rather than a seam.
        /// </summary>
        private static void AddSheetFixings(MeshBuilder mb)
        {
            float length = RafterLength;

            // Crests sit a quarter-pitch in from the sheet's edge and repeat at the
            // pitch. Every third one is fixed, which is the usual spacing and keeps
            // the count down: four purlins x two slopes x 30 crests would be 240
            // screws, and none of them is worth a draw call it does not need.
            const int EveryNthCrest = 3;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 n = SlopeNormal(side);
                Vector3 along = AlongSlope(side);
                // SlopeRotation already puts local +Y on the slope normal, which is
                // the axis a screw driven into the roof stands on.
                Quaternion upright = SlopeRotation(side);

                // On the crest line, so the fixing sits above the water rather than
                // in it - which is also why it reads as a dot rather than a seam.
                Vector3 crestPlane = RidgeStart(side)
                                     + (n * (Dim.RafterDepth + PurlinThickness + CorrugationAmplitude));

                foreach (float t in PurlinAlongSlope)
                {
                    if (t > length)
                    {
                        continue;
                    }

                    for (int c = 0; ; c += EveryNthCrest)
                    {
                        // Measured from the sheet's own starting edge, which is the
                        // gable the sheet was laid from - and that is a different end
                        // of the building on each slope.
                        float acrossSheet = (0.25f + c) * CorrugationPitch;
                        float z = side * (RoofHalfLength - acrossSheet);
                        if (acrossSheet > RoofHalfLength * 2f)
                        {
                            break;
                        }

                        Vector3 at = crestPlane + (along * t) + new Vector3(0f, 0f, z);
                        mb.AddCylinder(at + (n * 0.003f), 0.010f, 0.010f, 0.002f, 8, 0, upright);
                        mb.AddCylinder(at + (n * 0.006f), 0.005f, 0.004f, 0.005f, 6, 0, upright);
                    }
                }
            }
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
            // The sheet sits on the purlin at its troughs, so the trough line is
            // the purlin face - no amplitude term, which is the whole point of
            // lifting the sheet in BuildCoveringMesh.
            return UndersideY(x) + ((Dim.RafterDepth + PurlinThickness) / CosTheta);
        }
    }
}
