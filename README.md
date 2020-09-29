# **OCTOPUS**
This project is still not in v1, meaning the core isn't fully ready for any use yet. It will be ready soon though

 ==================
 
# *License GNU GPL v3, check out the full notice in LICENSE file*
 ==================


# **Own view about the project:**

> #### This project is rather ambitious from my point of view. I think it is the biggest project I've worked on until now.

> #### As the logo says: "Moving data from sea to sea" is a reference to the main functionality of this software, move any data from any db (or file) to any other db (or file) while keeping the fidelity of origin, for example if origin is a SQLite with a TEXT(20) it will create a new table in SQLServer as VARCHAR(20) (Or NVARCHAR in case it allows nulls)

> #### There's a lot of casuistic, that's why this project is designed in modules with the help of a json file for the definitions. Read [How to create a new Module] if you want to create a new one. As I said, the main plan is to allow the mobility of data from anywhere to everywhere you want but that's near impossible at this stage of the project, it needs a lot of definition and work, feel free to create your own modules for this project.

==================

# **Future of Octopus / My ideas**

* Add a design for the software, allowing non-tech users to use this piece of software
* Allow multiple origins
	* Capability to support any origin 
* Bulk creation of tables and moving the data (latter one optional)
* (I don't know how to say it any better) Allow the usage of the software as a library for anyone who wishes to implement a module and generate a DataTable (with columns and rows both optional) in a fast manner.
* Configuration to copy certain columns for each table
* Sync Mode: (Why deleting and inserting all over again when we can just sync the data)
	* Maybe scheduled tasks?
* Copy data from a table to a table with different column names, allowing user to determine the relation of columns with different names

==================

# **How to create your own module**

This project is structured to allow new modules to come into action, so in case there's an origin or destiny that are not yet developed you can make your own while still making use of the core of the software!

1. First of all is to add the new origin/destiny to the DbDefinitions.json
	1. name -> origin name
	2. fromDB -> treated as bool, true means you implemented the destiny mode (write)
	3. toDB -> treated as bool, true means you implemented the origin mode (read)
	4. className -> the name of the module, this one is important, It has to match with the actual method you create or else it won't be able to create the instance

2. The class has to inherit from the abstract class DataSource (which defines what methods are required, or else it won't work!)
	1. From here you can do stuff as you wish as long as you return a fully matured DataTable with DataColumns (with DataType as C# types, for that use the dictionary properties) and DataRows

3. (Optional) Don't forget to modify the keys fromDB or toDB in App.config if you wish to use the new module you created (as value use the name)

###### Octopus out

