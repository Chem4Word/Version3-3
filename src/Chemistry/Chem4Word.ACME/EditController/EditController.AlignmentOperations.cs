// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace Chem4Word.ACME
{
    public partial class EditController
    {
        #region Methods

        /// <summary>
        /// Aligns the middles of the selected objects
        /// along a horizontal line
        /// </summary>
        /// <param name="objects"></param>
        public void AlignMiddles(List<StructuralObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning middles of {objects.Count} objects");

                List<Transform> moleculeTransforms = new List<Transform>();
                List<Transform> annotationTransforms = new List<Transform>();
                List<Transform> reactionTransforms = new List<Transform>();

                List<Molecule> moleculesToAlign = objects.OfType<Molecule>().ToList();
                List<Annotation> annotationsToAlign = objects.OfType<Annotation>().ToList();
                List<Reaction> reactionsToAlign = objects.OfType<Reaction>().ToList();

                double molsMiddle = 0;
                double annotationsMiddle = 0;
                double reactionsMiddle = 0;

                if (moleculesToAlign.Any())
                {
                    molsMiddle = moleculesToAlign.Average(m => m.Centre.Y);
                }

                if (annotationsToAlign.Any())
                {
                    annotationsMiddle = annotationsToAlign.Average(a => (EditingCanvas.ChemicalVisuals[a].ContentBounds.Top +
                              EditingCanvas.ChemicalVisuals[a].ContentBounds.Bottom) / 2);
                }

                if (reactionsToAlign.Any())
                {
                    reactionsMiddle = reactionsToAlign.Average(r => (r.BoundingBox.Top + r.BoundingBox.Bottom) / 2);
                }

                double middle = (annotationsMiddle * annotationsToAlign.Count + molsMiddle * moleculesToAlign.Count +
                                 reactionsMiddle * reactionsToAlign.Count)
                                / (moleculesToAlign.Count + annotationsToAlign.Count + reactionsToAlign.Count);

                foreach (Molecule molecule in moleculesToAlign)
                {
                    TranslateTransform transform = new TranslateTransform { Y = middle - molecule.Centre.Y };
                    moleculeTransforms.Add(transform);
                }

                foreach (Annotation annotation in annotationsToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();
                    Annotation a = annotation;
                    transform.Y = middle - (EditingCanvas.ChemicalVisuals[a].ContentBounds.Top +
                                            EditingCanvas.ChemicalVisuals[a].ContentBounds.Bottom) / 2;
                    annotationTransforms.Add(transform);
                }

                foreach (Reaction reaction in reactionsToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();
                    Reaction a = reaction;
                    transform.Y = middle - (a.BoundingBox.Top + a.BoundingBox.Bottom) / 2;
                    reactionTransforms.Add(transform);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(moleculesToAlign, moleculeTransforms);
                AlignAnnotations(annotationsToAlign, annotationTransforms);
                AlignReactions(reactionsToAlign, reactionTransforms);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Aligns the tops of the selected objects
        /// along a horizontal line
        /// </summary>
        /// <param name="objects"></param>
        public void AlignTops(List<StructuralObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning tops of {objects.Count} objects");

                List<Transform> moleculeTransforms = new List<Transform>();
                List<Transform> annotationTransforms = new List<Transform>();

                List<Molecule> moleculesToAlign = objects.OfType<Molecule>().ToList();
                List<Annotation> annotationsToAlign = objects.OfType<Annotation>().ToList();

                double epsilon = 1.0E6;

                double top = Math.Min(moleculesToAlign.Select(m => m.Top)
                                                      .DefaultIfEmpty(epsilon).Min(),
                                      annotationsToAlign.Select(a => EditingCanvas.ChemicalVisuals[a].ContentBounds.Top)
                                                        .DefaultIfEmpty(epsilon).Min());

                foreach (Molecule molecule in moleculesToAlign)
                {
                    TranslateTransform transform = new TranslateTransform { Y = top - molecule.Top };
                    moleculeTransforms.Add(transform);
                }

                foreach (var annotation in annotationsToAlign)
                {
                    TranslateTransform transform = new TranslateTransform
                    {
                        Y = top - EditingCanvas.ChemicalVisuals[annotation].ContentBounds.Top
                    };
                    annotationTransforms.Add(transform);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(moleculesToAlign, moleculeTransforms);
                AlignAnnotations(annotationsToAlign, annotationTransforms);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Aligns a set of annotations according to the supplied transforms
        /// </summary>
        /// <param name="annotationsToAlign"></param>
        /// <param name="transforms"></param>
        public void AlignAnnotations(List<Annotation> annotationsToAlign, List<Transform> transforms)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Action redo = () =>
                              {
                                  for (int i = 0; i < annotationsToAlign.Count; i++)
                                  {
                                      annotationsToAlign[i].Position =
                                          transforms[i].Transform(annotationsToAlign[i].Position);
                                  }
                              };
                Action undo = () =>
                              {
                                  for (int i = 0; i < annotationsToAlign.Count; i++)
                                  {
                                      annotationsToAlign[i].Position = transforms[i].Inverse
                                          .Transform(annotationsToAlign[i].Position);
                                  }
                              };
                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
                redo();
                AddObjectListToSelection(annotationsToAlign.Cast<StructuralObject>().ToList());
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Aligns the bottoms of the selected objects
        /// along a horizontal line
        /// </summary>
        /// <param name="objects"></param>
        public void AlignBottoms(List<StructuralObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning bottoms of {objects.Count} objects");

                List<Transform> moleculeTransforms = new List<Transform>();
                List<Transform> annotationTransforms = new List<Transform>();

                List<Molecule> moleculesToAlign = objects.OfType<Molecule>().ToList();
                List<Annotation> annotationsToAlign = objects.OfType<Annotation>().ToList();

                double stupidMax = -100;

                double bottom = Math.Max(moleculesToAlign.Select(m => m.Bottom)
                                                         .DefaultIfEmpty(stupidMax).Max(),
                                         annotationsToAlign
                                             .Select(a => EditingCanvas.ChemicalVisuals[a].ContentBounds.Bottom)
                                             .DefaultIfEmpty(stupidMax).Max());

                foreach (Molecule molecule in moleculesToAlign)
                {
                    TranslateTransform transform = new TranslateTransform { Y = bottom - molecule.Bottom };
                    moleculeTransforms.Add(transform);
                }

                foreach (Annotation annotation in annotationsToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();
                    transform.Y = bottom - EditingCanvas.ChemicalVisuals[annotation].ContentBounds.Bottom;
                    annotationTransforms.Add(transform);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(moleculesToAlign, moleculeTransforms);
                AlignAnnotations(annotationsToAlign, annotationTransforms);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Aligns the centres of the selected objects
        /// along a vertical line
        /// </summary>
        /// <param name="objects"></param>
        public void AlignCentres(List<StructuralObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning centres of {objects.Count} objects");

                List<Transform> moleculeTransforms = new List<Transform>();
                List<Transform> annotationTransforms = new List<Transform>();
                List<Transform> reactionTransforms = new List<Transform>();

                List<Molecule> moleculesToAlign = objects.OfType<Molecule>().ToList();
                List<Annotation> annotationsToAlign = objects.OfType<Annotation>().ToList();
                List<Reaction> reactionsToAlign = objects.OfType<Reaction>().ToList();

                double molsCentre = 0;
                double annotationsCentre = 0;
                double reactionsCentre = 0;

                if (moleculesToAlign.Any())
                {
                    molsCentre = moleculesToAlign.Average(m => m.Centre.X);
                }

                if (annotationsToAlign.Any())
                {
                    annotationsCentre = annotationsToAlign
                        .Average(a => (EditingCanvas.ChemicalVisuals[a].ContentBounds.Left +
                                       EditingCanvas.ChemicalVisuals[a].ContentBounds.Right) / 2);
                }

                if (reactionsToAlign.Any())
                {
                    reactionsCentre = reactionsToAlign.Average(r => (r.BoundingBox.Left + r.BoundingBox.Right) / 2);
                }

                double centre = (annotationsCentre * annotationsToAlign.Count + molsCentre * moleculesToAlign.Count +
                                 reactionsCentre * reactionsToAlign.Count)
                                / (moleculesToAlign.Count + annotationsToAlign.Count + reactionsToAlign.Count);

                foreach (Molecule molecule in moleculesToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();

                    transform.X = centre - molecule.Centre.X;
                    moleculeTransforms.Add(transform);
                }

                foreach (Annotation annotation in annotationsToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();
                    Annotation a = annotation;
                    transform.X = centre - (EditingCanvas.ChemicalVisuals[a].ContentBounds.Left +
                                            EditingCanvas.ChemicalVisuals[a].ContentBounds.Right) / 2;
                    annotationTransforms.Add(transform);
                }

                foreach (Reaction reaction in reactionsToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();
                    Reaction a = reaction;
                    transform.X = centre - (a.BoundingBox.Left + a.BoundingBox.Right) / 2;
                    reactionTransforms.Add(transform);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(moleculesToAlign, moleculeTransforms);
                AlignAnnotations(annotationsToAlign, annotationTransforms);
                AlignReactions(reactionsToAlign, reactionTransforms);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Alights all reactions according to the supplied transforms
        /// </summary>
        /// <param name="reactionsToAlign"></param>
        /// <param name="transforms"></param>
        public void AlignReactions(List<Reaction> reactionsToAlign, List<Transform> transforms)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                Action redo = () =>
                              {
                                  for (int i = 0; i < reactionsToAlign.Count; i++)
                                  {
                                      reactionsToAlign[i].TailPoint =
                                          transforms[i].Transform(reactionsToAlign[i].TailPoint);
                                      reactionsToAlign[i].HeadPoint =
                                          transforms[i].Transform(reactionsToAlign[i].HeadPoint);
                                  }
                              };
                Action undo = () =>
                              {
                                  for (int i = 0; i < reactionsToAlign.Count; i++)
                                  {
                                      reactionsToAlign[i].TailPoint =
                                          transforms[i].Inverse.Transform(reactionsToAlign[i].TailPoint);
                                      reactionsToAlign[i].HeadPoint =
                                          transforms[i].Inverse.Transform(reactionsToAlign[i].HeadPoint);
                                  }
                              };
                UndoManager.BeginUndoBlock();
                UndoManager.RecordAction(undo, redo);
                UndoManager.EndUndoBlock();
                redo();
                AddObjectListToSelection(reactionsToAlign.Cast<StructuralObject>().ToList());
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Aligns the lefts of the selected objects
        /// along a vertical line
        /// </summary>
        /// <param name="objects"></param>
        public void AlignLefts(List<StructuralObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning lefts of {objects.Count} objects");

                List<Transform> moleculeTransforms = new List<Transform>();
                List<Transform> annotationTransforms = new List<Transform>();

                List<Molecule> moleculesToAlign = objects.OfType<Molecule>().ToList();
                List<Annotation> annotationsToAlign = objects.OfType<Annotation>().ToList();

                double stupidMin = 1.0E6;

                double left = Math.Min(moleculesToAlign.Select(m => m.Left)
                                                       .DefaultIfEmpty(stupidMin).Min(),
                                       annotationsToAlign
                                           .Select(a => EditingCanvas.ChemicalVisuals[a].ContentBounds.Left)
                                           .DefaultIfEmpty(stupidMin).Min());

                foreach (Molecule molecule in moleculesToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();
                    transform.X = left - molecule.Left;
                    moleculeTransforms.Add(transform);
                }

                foreach (Annotation annotation in annotationsToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();
                    transform.X = left - EditingCanvas.ChemicalVisuals[annotation].ContentBounds.Left;
                    annotationTransforms.Add(transform);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(moleculesToAlign, moleculeTransforms);
                AlignAnnotations(annotationsToAlign, annotationTransforms);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Aligns the rights of the selected objects
        /// along a vertical line
        /// </summary>
        /// <param name="objects"></param>
        public void AlignRights(List<StructuralObject> objects)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WriteTelemetry(module, "Debug", $"Aligning rights of {objects.Count} objects");

                List<Transform> moleculeTransforms = new List<Transform>();
                List<Transform> annotationTransforms = new List<Transform>();

                List<Molecule> moleculesToAlign = objects.OfType<Molecule>().ToList();
                List<Annotation> annotationsToAlign = objects.OfType<Annotation>().ToList();

                double stupidMax = -100;

                double right = Math.Max(moleculesToAlign.Select(m => m.Right)
                                                        .DefaultIfEmpty(stupidMax).Max(),
                                        annotationsToAlign
                                            .Select(a => EditingCanvas.ChemicalVisuals[a].ContentBounds.Right)
                                            .DefaultIfEmpty(stupidMax).Max());

                foreach (Molecule molecule in moleculesToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();
                    transform.X = right - molecule.Right;
                    moleculeTransforms.Add(transform);
                }

                foreach (Annotation annotation in annotationsToAlign)
                {
                    TranslateTransform transform = new TranslateTransform();
                    transform.X = right - EditingCanvas.ChemicalVisuals[annotation].ContentBounds.Right;
                    annotationTransforms.Add(transform);
                }

                UndoManager.BeginUndoBlock();
                AlignMolecules(moleculesToAlign, moleculeTransforms);
                AlignAnnotations(annotationsToAlign, annotationTransforms);
                UndoManager.EndUndoBlock();
            }
            catch (Exception exception)
            {
                WriteTelemetryException(module, exception);
            }
        }

        /// <summary>
        /// Aligns a set of molecules according to the supplied transforms
        /// </summary>
        /// <param name="molsToAlign"></param>
        /// <param name="transforms"></param>
        private void AlignMolecules(List<Molecule> molsToAlign, List<Transform> transforms)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            //first check to see whether we have an equal number of transforms and molecules
            if (molsToAlign.Count == transforms.Count)
            {
                UndoManager.BeginUndoBlock();
                MultiTransformMolecules(transforms, molsToAlign);
                AddObjectListToSelection(molsToAlign.Cast<StructuralObject>().ToList());
                UndoManager.EndUndoBlock();
            }
            else
            {
                WriteTelemetry(module, "Warning", "Number of transforms and molecules are not equal");
                WriteTelemetry(module, "StackTrace", Environment.StackTrace);
                Debugger.Break();
            }
        }

        #endregion Methods
    }
}
