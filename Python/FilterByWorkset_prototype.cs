public void FilterByWorksetName()
{
	// current UIDocument and Document
	UIDocument uidoc = this.ActiveUIDocument;
	Document doc = uidoc.Document;
	
	// Get the selection. Elements must be pre-selected before running this Macro.
	ICollection<ElementId> selectionIds = uidoc.Selection.GetElementIds();
	
	// Hard coded workset name to filter by.
	string worksetName = "AR - STRUCT";
	
	// Get the WorksetTable of the current document
	WorksetTable worksetTable = doc.GetWorksetTable();
	
	// Iterate through the selection and collect the elements that match the provided worksetName.
	List<ElementId> matchingElements = new List<ElementId>();
	foreach(ElementId eid in selectionIds)
	{
		// Get the WorksetId assigned the ElementId and retrieve the 
		// Workset from the WorksetTable.
		WorksetId wsId = doc.GetWorksetId(eid);
		Workset w = worksetTable.GetWorkset(wsId);
		
		// Check to see if the Element's Workset name matches the selected name.
		if(w.Name == worksetName)
			matchingElements.Add(eid);
	}
	
	// Change the current selection set to only be the elements that pass the filter.
	uidoc.Selection.SetElementIds(matchingElements);
}