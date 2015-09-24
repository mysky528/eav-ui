﻿{
  "General": {
    "Buttons": {
      "Add": "add",
      "Refresh": "refresh",
      "System": "advanced system functions",
      "Save": "save",
      "Cancel": "cancel",
      "Permissions": "permissions",
      "Edit": "edit",
      "Delete": "delete",
      "Copy": "copy"
    },
    "Messages": {
      "Loading": "loading...",
      "NothingFound": "no items found",
      "CantDelete": "can't delete {{target}}"
    },
    "Questions": {
      "Delete": "are you sure you want to delete {{target}}?",
      "SystemInput": "This is for very advanced operations. Only use this if you know what you're doing. \n\n Enter admin commands:"
    },
    "Terms": {
      "Title": "title"
    }
  },
  "ContentTypes": {
    "Title": "Content-Types and Data",
    "TypesTable": {
      "Name": "Name",
      "Description": "Description",
      "Fields": "Fields",
      "Items": "Items",
      "Actions": ""
    },
    "TitleExportImport": "Export / Import",
    "Buttons": {
      "Export": "Export",
      "Import": "Import"
    }
  },
  "ContentTypeEdit": {
    "Title": "Edit Content Type",
    "Name": "Name",
    "Description": "Description",
    "Scope": "Scope"
  },
  "Fields": {
    "Title": "Content Type Fields EN",
    "TitleEdit": "Add Fields",
    "Table": {
      "Title": "Title",
      "Name": "Static Name",
      "DataType": "Data Type",
      "Edit": "Edit & Data Type",
      "Label": "Label",
      "InputType": "Input Type",
      "Notes": "Notes",
      "Sort": "Sort",
      "Action": ""
    },
    "General": "General"
  },
  "Permissions": {
    "Title": "Permissions",
    "Table": {
      "Name": "Name",
      "Id": "ID",
      "Condition": "Condition",
      "Grant": "Grant",
      "Actions": ""
    }
  },
  "Pipeline": {
    "Manage": {
      "Title": "Visual Queries / Pipelines",
      "Intro": "Use the visual designer to create queries or merge data from various sources. This can then be used in views or accessed as JSON (if permissions allow). <a href='http://2sxc.org/en/help?tag=visualquerydesigner' target='_blank'>read more</a>",
      "Table": {
        "Id": "ID",
        "Name": "Name",
        "Description": "Description",
        "Actions": ""
      }

    },
    "Designer": {

    }
  },
  "Content": {
    "Manage": {
      "Title": "Manage Content / Data",
      "Table": {
        "Id": "ID",
        "Published": "Publ",
        "Title": "Title",
        "Actions": ""
      },
      "NoTitle": "- no title -"
    },
    "Publish": {
      "PnV": "published and visible",
      "DoP": "this is a draft of another published item",
      "D": "not published at the moment",
      "HD": "has draft: {{id}}",
      "HP": "will replace published: {{id}}"
    },
    "Export": {
      "Title": "Export Content / Data",
      "Help":  "",
      "Commands": {
        "Export": "Export",
        "Close": "Close"
      }
    },
    "Import": {
      "Title": "Import Content / Data Step {{step}} of 3",
      "Help": "This will import content-items into 2sxc. It requires that you already defined the content-type before you try importing, and that you created the import-file using the template provided by the Export. Please visit <a href='http://2sxc.org/help' target='_blank'>http://2sxc.org/help</a> for more instructions.",
      "Fields": {
        "File": {
          "Label": "Choose file"
        },
        "ResourcesReferences": {
          "Label": "References to pages / files",
          "Options": {
            "Keep": "Import links as written in the file (for example /Portals/...)",
            "Resolve": "Try to resolve pathes to references"
          }
        },
        "ClearEntities": {
          "Label": "Clear all other entities",
          "Options": {
            "None": "Keep all entities not found in import",
            "All": "Remove all entities not found in import"
          }
        }
      },
      "Commands": {
        "Preview": "Preview Import",
        "Import": "Import",
        "Back": "Back",
        "Close": "Close"
      },
      "Messages": {
        "BackupContentBefore": "Remember to backup your DNN first!",

        "WaitingForResponse": "Please wait while processing...",

        "ImportSucceeded": "Import done.",
        "ImportFailed": "Import failed.",

        "ImportCanTakeSomeTime": "Note: The import validates much data and may take several minutes."
      },
      "Evaluation": {
        "Error": {
          "Title": "Try to import file '{{filename}}'",
          "Codes": {
            "0": "Unknown error occured.",
            "1": "Selected content-type does not exist.",
            "2": "Document is not a valid XML file.",
            "3": "Selected content-type does not match the content-type in the XML file.",
            "4": "The language is not supported.",
            "5": "The document does not specify all languages for all entities.",
            "6": "Language reference cannot be parsed, the language is not supported.",
            "7": "Language reference cannot be parsed, the read-write protection is not supported.",
            "8": "Value cannot be read, because of it has an invalid format."
          },
          "Detail": "Details: {{detail}}",
          "LineNumber": "Line-no: {{number}}",
          "LineDetail": "Line-details: {{detail}}"
        },
        "Detail": {
          "Title": "Try to import file '{{filename}}'",
          "File": {
            "Title": "File contains:",
            "ElementCount": "{{count}} content-items (records/entities)",
            "LanguageCount": "{{count}} languages",
            "Attributes": "{{count}} columns: {{attributes}}"
          },
          "Entities": {
            "Title": "If you press Import, it will:",
            "Create": "Create {{count}} content-items",
            "Update": "Update {{count}} content-items",
            "Delete": "Delete {{count}} content-items",
            "AttributesIgnored": "Ignore {{count}} columns: {{attributes}}"
          }
        }
      }
    },
    "History": {
      "Title": "History of {{id}}",
      "Table": {
        "Id": "#",
        "When": "When",
        "User": "User",
        "Actions": ""
      }
    }
  }
}