import clr

# Add a reference to the RevitAPI and import the DB namespace.
clr.AddReference('RevitAPI')
from Autodesk.Revit.DB import *

# Add a refernce to RevitServices to gain access to the Revit Document.
# Then import the DocumentManager from RevitServices to get the document.
clr.AddReference('RevitServices')
from RevitServices.Persistence import DocumentManager

# The inputs to this node will be stored as a list in the IN variables.
# Instead of getting all of the inputs together, we'll grab them individually.
#dataEnteringNode = IN
elements = IN[0]
worksetName = IN[1]

# Get the document from the DocumentManager
doc = DocumentManager.Instance.CurrentDBDocument

# Get the WorksetTable from the document
worksetTable = doc.GetWorksetTable()

# Create lists to store the matching and unmatching elements.
# The two lists are to match the convention in the FilterByBoolMask node.
matchingElements = []
nonMatchingElements = []

# Iterate through the input Elements to compare the Element's Workset
for elem in elements:
	# A Revit Element within Dynamo is wrapped into a Revit.Elements.Element class
	# that contains the Autodesk.Revit.DB.Element object so we need to use the 
	# UwnrapElement method to get the DB element we can actually get properites from.
	rElem = UnwrapElement(elem)
	
	# Get the WorksetId from the Revit Element's Id property
	wsId = doc.GetWorksetId(rElem.Id)
	
	# Get the Workset from the WorksetTable using the Worksetid
	workset = worksetTable.GetWorkset(wsId)
	
	# check the name of the workset against the input name
	if workset.Name == worksetName:
		# Add the wrapped elem to the matching list.
		matchingElements.append(elem)
	else:
		# add the wrapped elem to the non-matching list.
		nonMatchingElements.append(elem)

#Assign your output to the OUT variable.
OUT = matchingElements, nonMatchingElements