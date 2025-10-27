# Intro

ACME uses a Model-View Controller (MVC) pattern to separate concerns and improve maintainability.
The key components are the **EditController**, **Model**, and **Display**.

## EditController
This mediates between the Display and the Model. It handles user inputs from the Display and updates the Model accordingly.

The Model is directly manipulated by the EditController. Any changes to the actual display itself come directly from the Model and bypass the EditController.
We use events to notify the Display of changes in the Model. The Model generates these events and they bubble up through the containment hierarchy.

The **EditController** got too big, so we broke it up as follows:

::: mermaid
graph TD
	EC["EditController"] --> ED{"EditController.cs"}
	EC--> CON["EditController.Constructors.cs"]
	EC--> SEL["EditController.Selection.cs"]
	ED--> PROPS["EditController.Properties.cs"]	
	ED--> AE["EditController.AtomEditing.cs"]
	ED--> BE["EditController.BondEditing.cs"]
	ED--> ME["EditController.MoleculeEditing.cs"]
	ED--> BASE["EditController.BasicEditing.cs"]
	EC--> SM["EditController.StateManagement.cs"]
	EC--> RX["EditController.Reactions.cs"]
	EC--> AOPS["EditController.AlignmentOperations.cs"]
:::

EditController.cs is a small file containing some very basic operations that don't fit elsewhere.
The other files are grouped by functionality to make it easier to find and maintain code.
All files contain a mixture of methods, properties, events and commands.
