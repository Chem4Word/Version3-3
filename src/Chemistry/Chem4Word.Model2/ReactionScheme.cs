﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Chem4Word.Model2
{
    /* NB: the Model currently supports only one reaction scheme. This should *always* be
     * access by the DefaultReactionScheme property. Do not add more schemes to the Model!
     * We will review support for additional schemes as and when appropriate */

    public class ReactionScheme : INotifyPropertyChanged
    {
        public readonly ReadOnlyDictionary<Guid, Reaction> Reactions;
        private readonly Dictionary<Guid, Reaction> _reactions;
        public string Id { get; set; }
        public Guid InternalId { get; internal set; }
        public Model Parent { get; set; }

        public string Path
        {
            get
            {
                string path = "";

                if (Parent == null)
                {
                    path = Id;
                }

                if (Parent is Model model)
                {
                    path = model.Path + Id;
                }

                return path;
            }
        }

        public event NotifyCollectionChangedEventHandler ReactionsChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public ReactionScheme()
        {
            InternalId = Guid.NewGuid();
            Id = InternalId.ToString("D");

            _reactions = new Dictionary<Guid, Reaction>();
            Reactions = new ReadOnlyDictionary<Guid, Reaction>(_reactions);
        }

        public void AddReaction(Reaction reaction)
        {
            _reactions[reaction.InternalId] = reaction;
            NotifyCollectionChangedEventArgs e =
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                     new List<Reaction> { reaction });
            OnReactionsChanged(this, e);
            UpdateReactionEventHandlers(e);
        }

        //transmits a reaction being added or removed
        private void OnReactionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler temp = ReactionsChanged;
            if (temp != null)
            {
                temp.Invoke(sender, e);
            }
        }

        public void RemoveReaction(Reaction reaction)
        {
            bool result = _reactions.Remove(reaction.InternalId);
            if (result)
            {
                NotifyCollectionChangedEventArgs e =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                         new List<Reaction> { reaction });
                OnReactionsChanged(this, e);
                UpdateReactionEventHandlers(e);
            }
        }

        private void UpdateReactionEventHandlers(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    var r = (Reaction)oldItem;
                    r.ReactantsChanged -= OnReactantsChanged_Reaction;
                    r.ProductsChanged -= OnProductsChanged_Reaction;
                    r.PropertyChanged -= OnPropertyChanged_Reaction;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    var r = (Reaction)newItem;
                    r.ReactantsChanged += OnReactantsChanged_Reaction;
                    r.ProductsChanged += OnProductsChanged_Reaction;
                    r.PropertyChanged += OnPropertyChanged_Reaction;
                }
            }
        }

        private void OnPropertyChanged_Reaction(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        private void OnProductsChanged_Reaction(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Don't do anything for now
        }

        private void OnReactantsChanged_Reaction(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Don't do anything for now
        }

        public void ReLabel(ref int schemeCount, ref int reactionCount)
        {
            Id = $"rs{++schemeCount}";

            foreach (Reaction r in Reactions.Values)
            {
                r.Id = $"r{++reactionCount}";
            }
        }

        public ReactionScheme Copy(Model modelCopy = null)
        {
            ReactionScheme copy = new ReactionScheme();
            copy.Id = Id;

            foreach (var reaction in Reactions.Values)
            {
                Reaction r = reaction.Copy(modelCopy);
                copy.AddReaction(r);
                r.Parent = copy;
            }
            return copy;
        }

        public void RepositionAll(double x, double y)
        {
            foreach (Reaction r in Reactions.Values)
            {
                r.RepositionAll(x, y);
            }
        }

        internal void ReLabelGuids(ref int reactionSchemeCount, ref int reactionCount)
        {
            Guid guid;
            if (Guid.TryParse(Id, out guid))
            {
                Id = $"rs{++reactionSchemeCount}";
            }
            foreach (Reaction r in Reactions.Values)
            {
                r.ReLabelGuids(ref reactionCount);
            }
        }

        public void SetMissingIds()
        {
            foreach (var reaction in Reactions.Values)
            {
                if (string.IsNullOrEmpty(reaction.Id))
                {
                    // Don't do anything for now
                }
            }
        }
    }
}