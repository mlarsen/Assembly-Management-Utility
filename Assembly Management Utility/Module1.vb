﻿Imports System.Data.Odbc
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.IO.Compression
Module Module1
    Public sqlArray(10) As String
    'Public stPath As String = "E:\Core\HeltonTools\CSEngine"
    Public stPath As String = System.AppDomain.CurrentDomain.BaseDirectory()
    Public stODBCString As String
    Public bolIsClientServer As Boolean
    Public bolIsError As Boolean = False
    Public stAssemblyProd As String
    Public stAssemblyKit As String
    Public iAdjustQty As Integer
    Public stAssemblyFilter As String
    Public stKitSuffix As String
    Sub LoadForm()
        Dim stSQL As String

        stSQL = "SELECT DISTINCT ProductNo,Description,InStock FROM Products WHERE ProductNo LIKE '" + stAssemblyFilter + "' AND ProductNo NOT LIKE '%-" + stKitSuffix + "';"
        LoadCombo(Form1.cmboSelectAssembly, stSQL)
    End Sub

    Function GetSettings(ByRef stExePath As String) As String
        Dim stIniFilePath As String = stExePath + "AssemblyManagement.ini"
        Dim stIniFile = New IniFile()



        If stIniFilePath <> "" Then
            stIniFile.Load(stIniFilePath)
            stAssemblyFilter = stIniFile.GetKeyValue("Settings", "AssemblyFilter")
            stKitSuffix = stIniFile.GetKeyValue("Settings", "KitSuffix")
        Else
            stAssemblyFilter = ""
        End If
        Return stAssemblyFilter

    End Function
    Sub CreateUpdateIniFile(ByRef stKey As String, ByRef stValue As String)
        Dim stInifilePath As String = stPath + "AssemblyManagement.ini"
        Dim stIniFile = New IniFile()

        If My.Computer.FileSystem.FileExists(stInifilePath) = False Then
            Using sw As New System.IO.StreamWriter(stInifilePath, False)
                sw.WriteLine("[Settings]")
            End Using
        End If

        stIniFile.Load(stInifilePath)
        stIniFile.SetKeyValue("Settings", stKey, stValue)
        stIniFile.Save(stInifilePath)

    End Sub
    Sub AdjustKit()
        Dim stISQL As String = "INSERT INTO KitAdjustments(ProductNo,InStock,AdjustBy,NewInStock,AddProductNo,AddProdInStock,AddProdNewInstock,KitAdjustQty,UnitCost,LocationNo) VALUES("
        Dim stASQL As String = "INSERT INTO TR_AdjustInventory(ProductNo,Form,OperatorID,RequestAdjustBy,IsForceAdjust,Cost,LocationNo) "
        Dim stSQL As String
        Dim iQuantity As Integer
        Dim iInStock As Integer
        Dim iKitAdjustBy As Integer
        Dim stAddProductNo As String
        Dim flUnitCost As Decimal
        Dim iNewInStock As Integer
        Dim bolIsMultipleLocations As Boolean = False
        Dim stLocationNo As String = "NULL"

        'Code added 9/5/2019: Create a temp table for storing the kit Adjustment information for the session
        CreateTempTable()

        'Code added 6/17/19: Check for multiple Locations
        stSQL = "SELECT IsMultipleLocations FROM Products WHERE ProductNo='" + stAssemblyProd + "';"
        SQLTextQuery("S", stSQL, stODBCString, 1)
        If sqlArray(0) = "True" Then
            bolIsMultipleLocations = True
            stSQL = "SELECT LocationNo FROM Locations WHERE Description='Default';"
            SQLTextQuery("S", stSQL, stODBCString, 1)
            If sqlArray(0) <> "NoData" Then
                stLocationNo = sqlArray(0)
            Else
                stLocationNo = "NULL"
            End If
        End If

        'Adjust the master product
        If iAdjustQty > 0 Then
            stSQL = stASQL + "VALUES('" + stAssemblyProd + "','Assemby Management',999," + CStr(iAdjustQty) + ",False,NULL," + stLocationNo + ");"
        Else
            stSQL = stASQL + "VALUES('" + stAssemblyProd + "','Assemby Management',999," + CStr(iAdjustQty) + ",False,NULL," + stLocationNo + ");"
        End If

        SQLTextQuery("U", stSQL, stODBCString, 0)


        'Adjust the assembly products
        For i = 0 To Form1.DataGridView1.Rows.Count - 1
            iQuantity = Form1.DataGridView1.Rows(i).Cells("AssemblyQty").Value
            iKitAdjustBy = iAdjustQty * iQuantity
            iInStock = Form1.DataGridView1.Rows(i).Cells("InStock").Value
            stAddProductNo = Form1.DataGridView1.Rows(i).Cells("ProductNo").Value
            flUnitCost = Form1.DataGridView1.Rows(i).Cells("UnitCost").Value
            iNewInStock = Form1.DataGridView1.Rows(i).Cells("NewInStock").Value

            'Code added 6/17/19: Check for multiple locations
            bolIsMultipleLocations = False
            stSQL = "SELECT IsMultipleLocations FROM Products WHERE ProductNo='" + stAddProductNo + "';"
            SQLTextQuery("S", stSQL, stODBCString, 1)
            If sqlArray(0) = "True" Then
                bolIsMultipleLocations = True
                stSQL = "SELECT LocationNo FROM Locations WHERE Description='Default';"
                SQLTextQuery("S", stSQL, stODBCString, 1)
                If sqlArray(0) <> "NoData" Then
                    stLocationNo = sqlArray(0)
                Else
                    stLocationNo = "NULL"
                End If

            End If

            If iKitAdjustBy < 0 Then
                stSQL = stASQL + "VALUES('" + stAddProductNo + "','Assemby Management',999," + CStr(-1 * iKitAdjustBy) + ",False," + CStr(flUnitCost) + "," + stLocationNo + ");"
            Else
                stSQL = stASQL + "VALUES('" + stAddProductNo + "','Assemby Management',999," + CStr(-1 * iKitAdjustBy) + ",False," + CStr(flUnitCost) + "," + stLocationNo + ");"
            End If
            SQLTextQuery("U", stSQL, stODBCString, 0)


            'Add the record to KitAdjustments
            stSQL = stISQL + "'" + stAssemblyProd + "'," + Form1.txtInStock.Text + "," + CStr(iAdjustQty) + "," + Form1.txtNewInstock.Text + ",'" + stAddProductNo + "'," + CStr(iInStock) + "," + CStr(iInStock - iKitAdjustBy) + "," + CStr(-1 * iKitAdjustBy) + "," + CStr(flUnitCost) + "," + stLocationNo + ");"
            SQLTextQuery("U", stSQL, stODBCString, 0)

            'Code added 9/5/19: Add the data to the TempKitAdjustments table
            'stSQL = stTSQL + "'" + stAssemblyProd + "'," + Form1.txtInStock.Text + "," + CStr(iAdjustQty) + "," + Form1.txtNewInstock.Text + ",'" + stAddProductNo + "'," + CStr(iInStock) + "," + CStr(iInStock - iKitAdjustBy) + "," + CStr(-1 * iKitAdjustBy) + ");"
            'SQLTextQuery("U", stSQL, stODBCString, 0)

        Next

    End Sub
    Sub OldAdjustKit()
        Dim stISQL As String = "INSERT INTO KitAdjustments(ProductNo,InStock,AdjustBy,NewInStock,AddProductNo,AddProdInStock,AddProdNewInstock,KitAdjustQty,UnitCost) VALUES("
        'Dim stTSQL As String = "INSERT INTO TempKitAdjustments(ProductNo,InStock,AdjustBy,NewInStock,AddProductNo,AddProdInStock,AddProdNewInstock,KitAdjustQty) VALUES("
        Dim stSQL As String
        Dim iQuantity As Integer
        Dim iInStock As Integer
        Dim iKitAdjustBy As Integer
        Dim stAddProductNo As String
        Dim flUnitCost As Decimal
        Dim flAverageCost As Decimal
        Dim iNewInStock As Integer
        Dim stLocStockInStock As String
        Dim bolIsMultipleLocations As Boolean = False

        'Code added 9/5/2019: Create a temp table for storing the kit Adjustment information for the session
        CreateTempTable()

        'Code added 6/17/19: Check for multiple Locations
        stSQL = "SELECT IsMultipleLocations FROM Products WHERE ProductNo='" + stAssemblyProd + "';"
        SQLTextQuery("S", stSQL, stODBCString, 1)
        If sqlArray(0) = "True" Then
            bolIsMultipleLocations = True
        End If

        'Adjust the master product
        If iAdjustQty > 0 Then
            If bolIsMultipleLocations = False Then
                stSQL = "UPDATE Products SET InStock=InStock+" + CStr(iAdjustQty) + " WHERE ProductNo='" + stAssemblyProd + "';"
            Else
                stSQL = "UPDATE LocStock S SET InStock=InStock+" + CStr(iAdjustQty) + " FROM LocStock S JOIN Location L ON S.LocationNo=L.LocationNo WHERE L.Description='Default' AND S.ProductNo='" + stAssemblyProd + "';"
            End If

        Else
            If bolIsMultipleLocations = False Then
                stSQL = "UPDATE Products SET InStock=InStock" + CStr(iAdjustQty) + " WHERE ProductNo='" + stAssemblyProd + "';"
            Else
                stSQL = "UPDATE LocStock S SET InStock=InStock" + CStr(iAdjustQty) + " FROM LocStock S JOIN Location L ON S.LocationNo=L.LocationNo WHERE L.Description='Default' AND S.ProductNo='" + stAssemblyProd + "';"
            End If

        End If

        SQLTextQuery("U", stSQL, stODBCString, 0)


        'Adjust the assembly products
        For i = 0 To Form1.DataGridView1.Rows.Count - 1
            iQuantity = Form1.DataGridView1.Rows(i).Cells("AssemblyQty").Value
            iKitAdjustBy = iAdjustQty * iQuantity
            iInStock = Form1.DataGridView1.Rows(i).Cells("InStock").Value
            stAddProductNo = Form1.DataGridView1.Rows(i).Cells("ProductNo").Value
            flUnitCost = Form1.DataGridView1.Rows(i).Cells("UnitCost").Value
            iNewInStock = Form1.DataGridView1.Rows(i).Cells("NewInStock").Value

            'Code added 6/17/19: Check for multiple locations
            bolIsMultipleLocations = False
            stSQL = "SELECT IsMultipleLocations FROM Products WHERE ProductNo='" + stAddProductNo + "';"
            SQLTextQuery("S", stSQL, stODBCString, 1)
            If sqlArray(0) = "True" Then
                bolIsMultipleLocations = True
            End If

            If iKitAdjustBy < 0 Then

                If bolIsMultipleLocations = False Then
                    stSQL = "UPDATE Products SET InStock=InStock+" + CStr(-1 * iKitAdjustBy) + " WHERE ProductNo='" + stAddProductNo + "';"
                    SQLTextQuery("U", stSQL, stODBCString, 0)
                Else
                    stSQL = "UPDATE LocStock S SET InStock=InStock+" + CStr(-1 * iKitAdjustBy) + " FROM LocStock S JOIN Location L ON S.LocationNo=L.LocationNo WHERE L.Description='Default' AND S.ProductNo='" + stAddProductNo + "';"
                    SQLTextQuery("U", stSQL, stODBCString, 0)

                    'Code added 03/11/2020: Update the Products InStock quantity for computing the Average Cost
                    stSQL = "SELECT SUM(InStock) FROM LocStock WHERE IsNonInventory<>True AND IsNotAvailableForSale<>True AND ProductNo='" + stAddProductNo + "';"
                    SQLTextQuery("S", stSQL, stODBCString, 1)
                    If sqlArray(0) <> "NoData" Then
                        stLocStockInStock = sqlArray(0)
                        stSQL = "UPDATE Products SET InStock=" + stLocStockInStock + " WHERE ProductNo='" + stAddProductNo + "';"
                        SQLTextQuery("U", stSQL, stODBCString, 0)
                    End If
                End If

                'Code added 03/11/2020: Compute the Average Cost and update the Products.AverageCost
                flAverageCost = ComputeAverageCost(stAddProductNo, iQuantity, flUnitCost)
                If flAverageCost <> -1 Then 'If there were no errors
                    stSQL = "UPDATE Products SET AverageCost=" + CStr(flAverageCost) + " WHERE ProductNo='" + stAddProductNo + "';"
                    SQLTextQuery("U", stSQL, stODBCString, 0)
                End If
            Else


                If bolIsMultipleLocations = False Then
                    stSQL = "UPDATE Products SET InStock=InStock" + CStr(-1 * iKitAdjustBy) + " WHERE ProductNo='" + stAddProductNo + "';"
                    SQLTextQuery("U", stSQL, stODBCString, 0)
                Else
                    stSQL = "UPDATE LocStock S SET InStock=InStock" + CStr(-1 * iKitAdjustBy) + " FROM LocStock S JOIN Location L ON S.LocationNo=L.LocationNo WHERE L.Description='Default' AND S.ProductNo='" + stAddProductNo + "';"
                    SQLTextQuery("U", stSQL, stODBCString, 0)
                End If

            End If



            'Add the record to KitAdjustments
            stSQL = stISQL + "'" + stAssemblyProd + "'," + Form1.txtInStock.Text + "," + CStr(iAdjustQty) + "," + Form1.txtNewInstock.Text + ",'" + stAddProductNo + "'," + CStr(iInStock) + "," + CStr(iInStock - iKitAdjustBy) + "," + CStr(-1 * iKitAdjustBy) + "," + CStr(flUnitCost) + ");"
            SQLTextQuery("U", stSQL, stODBCString, 0)

            'Code added 9/5/19: Add the data to the TempKitAdjustments table
            'stSQL = stTSQL + "'" + stAssemblyProd + "'," + Form1.txtInStock.Text + "," + CStr(iAdjustQty) + "," + Form1.txtNewInstock.Text + ",'" + stAddProductNo + "'," + CStr(iInStock) + "," + CStr(iInStock - iKitAdjustBy) + "," + CStr(-1 * iKitAdjustBy) + ");"
            'SQLTextQuery("U", stSQL, stODBCString, 0)

        Next

    End Sub
    Function ComputeAverageCost(ByRef stProductNo As String, ByRef iQuantity As Integer, ByRef flUnitPrice As Decimal) As Decimal
        Dim stSQL As String
        Dim flUnitCost As Decimal = iQuantity * flUnitPrice
        Dim iTotalInStock As Integer
        Dim flChannergyCost As Decimal
        Dim flAverageCost As Decimal

        'Check to see if the flUnitPrice is zero, if so return -1
        If flUnitPrice = 0 Then
            Return -1
        End If

        'Get the cost of what is in inventory
        stSQL = "SELECT InStock,InStock*AverageCost FROM Products WHERE ProductNo='" + stProductNo + "';"
        SQLTextQuery("S", stSQL, stODBCString, 2)
        If sqlArray(0) <> "NoData" Then
            iTotalInStock = CInt(sqlArray(0))
            flChannergyCost = CDec(sqlArray(1))
            flAverageCost = ((flUnitCost * iQuantity) + (flChannergyCost * iTotalInStock)) / (iTotalInStock + iQuantity)
            Return flAverageCost
        Else
            Return -1
        End If
    End Function
    Sub CreateTempTable()
        Dim stSQL As String

        stSQL = "DROP TABLE If EXISTS TempKitAdjustments;"
        SQLTextQuery("U", stSQL, stODBCString, 0)


        stSQL = "CREATE TABLE IF Not EXISTS TempKitAdjustments ("
        stSQL = stSQL + "AdjustmentNo AUTOINC,"
        stSQL = stSQL + "ProductNo VARCHAR(100),"
        stSQL = stSQL + "InStock FLOAT,"
        stSQL = stSQL + "AdjustBy FLOAT,"
        stSQL = stSQL + "NewInStock FLOAT,"
        stSQL = stSQL + "AddProductNo VARCHAR(100),"
        stSQL = stSQL + "AddProdInStock FLOAT,"
        stSQL = stSQL + "KitAdjustQty INTEGER,"
        stSQL = stSQL + "KitUnitCost MONEY,"
        stSQL = stSQL + "AddProdNewInStock FLOAT,"
        stSQL = stSQL + "AdjustDateTime TIMESTAMP DEFAULT CURRENT_TIMESTAMP,"
        stSQL = stSQL + "AdjustDate DATE DEFAULT CURRENT_DATE);"
        SQLTextQuery("U", stSQL, stODBCString, 0)

    End Sub
    Sub CreateTables()
        Dim stSQL As String

        stSQL = "CREATE TABLE IF Not EXISTS KitAdjustments ("
        stSQL = stSQL + "AdjustmentNo AUTOINC,"
        stSQL = stSQL + "ProductNo VARCHAR(100),"
        stSQL = stSQL + "InStock FLOAT,"
        stSQL = stSQL + "AdjustBy FLOAT,"
        stSQL = stSQL + "NewInStock FLOAT,"
        stSQL = stSQL + "AddProductNo VARCHAR(100),"
        stSQL = stSQL + "AddProdInStock FLOAT,"
        stSQL = stSQL + "AddProdNewInStock FLOAT,"
        stSQL = stSQL + "AdjustDateTime TIMESTAMP DEFAULT CURRENT_TIMESTAMP);"

        SQLTextQuery("U", stSQL, stODBCString, 0)

        'Add the Adjust time field
        stSQL = "ALTER TABLE KitAdjustments "
        stSQL = stSQL + "ADD COLUMN IF NOT EXISTS AdjustDate DATE DEFAULT CURRENT_DATE, "
        stSQL = stSQL + "ADD COLUMN IF NOT EXISTS KitAdjustQty INTEGER AT 7, "
        stSQL = stSQL + "ADD COLUMN IF NOT EXISTS LocationNo INTEGER;"
        SQLTextQuery("U", stSQL, stODBCString, 0)

        'Add the UnitCost field
        stSQL = "ALTER TABLE KitAdjustments "
        stSQL = stSQL + "ADD COLUMN IF Not EXISTS UnitCost FLOAT;"
        SQLTextQuery("U", stSQL, stODBCString, 0)

    End Sub
    Sub LoadDataGridViewSQL(ByRef stSQL As String, ByRef GV As DataGridView)


        Dim da As New OdbcDataAdapter(stSQL, stODBCString)
        Dim ds As New DataSet()
        Dim dt As New DataTable

        da.Fill(ds, "logn")
        GV.DataSource = ds.Tables(0)

        'Set the read only and visibility
        For i = 0 To ds.Tables(0).Columns.Count - 1
            If Right(GV.Columns(i).HeaderText, 2) = "No" And GV.Columns(i).HeaderText <> "ProductNo" Then
                GV.Columns(i).Visible = False
            End If

            If GV.Columns(i).HeaderText = "ProductNo" Then
                GV.Columns(i).ReadOnly = True

            End If
        Next


    End Sub



    Sub LoadCombo(ByRef ComboBox As ComboBox, ByRef stSQL As String)
        Dim dt As New DataTable
        Dim da As New OdbcDataAdapter
        Dim con As New OdbcConnection(stODBCString)
        Dim i As Integer

        da = New OdbcDataAdapter(stSQL, con)
        da.Fill(dt)

        ComboBox.Items.Clear()

        For i = 0 To dt.Rows.Count - 1
            ComboBox.Items.Add(dt.Rows(i).Item(0).ToString)
        Next

        con.Close()
    End Sub

    Sub SQLTextQuery(ByVal QueryType As String, ByVal CommandText As String, ByVal stODBC As String, Optional ByVal Columns As Integer = 0)
        'Dim DBCString As String = "Driver= {C: \dbisam\odbc\std\ver4\lib\dbodbc\dbodbc.dll};connectiontype=Local;remoteipaddress=127.0.0.1;RemotePort=12005;remotereadahead=50;catalogname=" + stODBCString + ";readonly=False;lockretrycount=15;lockwaittime=100;forcebufferflush=False;strictchangedetection=False;"

        'Dim DBC As New System.Data.Odbc.OdbcConnection
        'DBC.ConnectionString = DBCString

        Dim DBC As New OdbcConnection(stODBC)
        'Dim DBCString As String = "Dsn=" & stODBC & ";"

        'Dim DBC As New OdbcConnection(DBCString)

        If QueryType = "S" And Columns > 0 Then
            Try
                Dim SQL1 As New OdbcCommand
                SQL1.Connection = DBC
                SQL1.CommandType = CommandType.Text
                SQL1.CommandText = CommandText
                DBC.Open()

                Dim DataRow As OdbcDataReader
                DataRow = SQL1.ExecuteReader()
                DataRow.Read()
                If DataRow.HasRows Then
                    Dim Counter As Integer = 0
                    While Counter < Columns
                        sqlArray(Counter) = DataRow(Counter).ToString
                        Counter = Counter + 1
                    End While
                Else
                    sqlArray(0) = "NoData"
                End If
                DataRow.Close()
                DBC.Close()

                SQL1.Dispose()

            Catch ex As Exception
                MessageBox.Show("SQL Error: " + ex.Message)
                bolIsError = True

            End Try
        End If
        If QueryType = "U" Or QueryType = "I" Or QueryType = "D" Then
            Try
                DBC.Open()
                Dim SQL2 As New OdbcCommand
                SQL2.Connection = DBC
                SQL2.CommandType = CommandType.Text
                SQL2.CommandTimeout = 60
                SQL2.CommandText = CommandText
                SQL2.ExecuteScalar()
                DBC.Close()
                SQL2.Dispose()

            Catch ex As Exception
                MessageBox.Show("SQL Error: " + ex.Message)

                bolIsError = True

            End Try
        End If
    End Sub
    Function GetDSN(ByRef stDataPath As String) As String
        Dim stDSN As String
        Dim stIniFile = New IniFile()
        Dim stIniFilePath As String = stDataPath + "clientserver.ini"
        Dim stIpAddress As String
        Dim stCatalog As String
        Dim builder As New OdbcConnectionStringBuilder()

        builder.Driver = "DBISAM 4 ODBC Driver"

        'Check to see if the clientserver.ini file is in the passed path
        If My.Computer.FileSystem.FileExists(stIniFilePath) = True Then
            stIniFile.Load(stIniFilePath)
            stIpAddress = stIniFile.GetKeyValue("Settings", "IPAddress")
            stCatalog = stIniFile.GetKeyValue("Settings", "RemoteDatabase")
            builder.Add("UID", "Admin")
            builder.Add("PWD", "DBAdmin")
            builder.Add("ConnectionType", "Remote")
            builder.Add("RemoteIPAddress", stIpAddress)
            builder.Add("CatalogName", stCatalog)
            builder.Add("TablePassword1", "&$LOU872n5")
            builder.Add("TablePassword2", "RTL78ESASIDJG")
            builder.Add("TablePassword3", "C9*hdlYynp0!9")
            stDSN = builder.ConnectionString
            bolIsClientServer = True
        ElseIf My.Computer.FileSystem.FileExists(stDataPath + "Version.dat") = True Then
            builder.Add("ConnectionType", "Local")
            builder.Add("CatalogName", stDataPath)
            builder.Add("TablePassword1", "&$LOU872n5")
            builder.Add("TablePassword2", "RTL78ESASIDJG")
            builder.Add("TablePassword3", "C9*hdlYynp0!9")
            stDSN = builder.ConnectionString
        Else
            stDSN = ""
        End If

        Return stDSN
    End Function
    Function IsLicensed(ByRef stApplication As String) As Boolean
        Dim stSQL As String = "SELECT IsLicensed FROM CTCtrl WHERE REPLACE(#39 WITH '' IN LicensedTo)='" + GetCompany() + "' AND Application='" + stApplication + "';"

        Try
            SQLTextQuery("S", stSQL, stODBCString, 1)
            If sqlArray(0) = "True" Then
                Return True
            Else
                Return False
            End If

        Catch ex As Exception
            Return False
        End Try



    End Function
    Function GetCompany() As String
        Dim stSQL As String = "SELECT " + Chr(34) + "Licensed To" + Chr(34) + " FROM MwCtrl;"
        Dim stLine() As String
        Dim stCompany As String

        SQLTextQuery("S", stSQL, stODBCString, 1)

        If sqlArray(0) <> "" Then
            stLine = Strings.Split(sqlArray(0), vbCrLf)
            stCompany = StripQuotes(stLine(0))
        Else
            stCompany = ""
        End If
        Return stCompany
    End Function
    Function StripQuotes(ByRef stString) As String
        Return Replace(stString, "'", "")
    End Function
    ' IniFile class used to read and write ini files by loading the file into memory
    Public Class IniFile
        ' List of IniSection objects keeps track of all the sections in the INI file
        Private m_sections As Hashtable

        ' Public constructor
        Public Sub New()
            m_sections = New Hashtable(StringComparer.InvariantCultureIgnoreCase)
        End Sub

        ' Loads the Reads the data in the ini file into the IniFile object
        Public Sub Load(ByVal sFileName As String, Optional ByVal bMerge As Boolean = False)
            If Not bMerge Then
                RemoveAllSections()
            End If
            '  Clear the object... 
            Dim tempsection As IniSection = Nothing
            Dim oReader As New StreamReader(sFileName)
            Dim regexcomment As New Regex("^([\s]*#.*)", (RegexOptions.Singleline Or RegexOptions.IgnoreCase))
            ' Broken but left for history
            'Dim regexsection As New Regex("\[[\s]*([^\[\s].*[^\s\]])[\s]*\]", (RegexOptions.Singleline Or RegexOptions.IgnoreCase))
            Dim regexsection As New Regex("^[\s]*\[[\s]*([^\[\s].*[^\s\]])[\s]*\][\s]*$", (RegexOptions.Singleline Or RegexOptions.IgnoreCase))
            Dim regexkey As New Regex("^\s*([^=\s]*)[^=]*=(.*)", (RegexOptions.Singleline Or RegexOptions.IgnoreCase))
            While Not oReader.EndOfStream
                Dim line As String = oReader.ReadLine()
                If line <> String.Empty Then
                    Dim m As Match = Nothing
                    If regexcomment.Match(line).Success Then
                        m = regexcomment.Match(line)
                        Trace.WriteLine(String.Format("Skipping Comment: {0}", m.Groups(0).Value))
                    ElseIf regexsection.Match(line).Success Then
                        m = regexsection.Match(line)
                        Trace.WriteLine(String.Format("Adding section [{0}]", m.Groups(1).Value))
                        tempsection = AddSection(m.Groups(1).Value)
                    ElseIf regexkey.Match(line).Success AndAlso tempsection IsNot Nothing Then
                        m = regexkey.Match(line)
                        Trace.WriteLine(String.Format("Adding Key [{0}]=[{1}]", m.Groups(1).Value, m.Groups(2).Value))
                        tempsection.AddKey(m.Groups(1).Value).Value = m.Groups(2).Value
                    ElseIf tempsection IsNot Nothing Then
                        '  Handle Key without value
                        Trace.WriteLine(String.Format("Adding Key [{0}]", line))
                        tempsection.AddKey(line)
                    Else
                        '  This should not occur unless the tempsection is not created yet...
                        Trace.WriteLine(String.Format("Skipping unknown type of data: {0}", line))
                    End If
                End If
            End While
            oReader.Close()
        End Sub
        ' Used to save the data back to the file or your choice
        Public Sub ListSections(ByVal sSection As String)
            Dim Counter As Integer = 0

            For Each s As IniSection In Sections
                If s.Name = sSection Then
                    For Each k As IniSection.IniKey In s.Keys
                        If k.Value <> String.Empty Then
                            'stCompanyList(Counter, 0) = k.Name
                            'stCompanyList(Counter, 1) = k.Value
                            'stCompanyList(Counter, 2) = CStr(Counter + 1)
                            Counter = Counter + 1
                        End If
                    Next
                End If
            Next

        End Sub
        ' Used to save the data back to the file or your choice
        Public Sub Save(ByVal sFileName As String)
            Dim oWriter As New StreamWriter(sFileName, False)
            For Each s As IniSection In Sections
                Trace.WriteLine(String.Format("Writing Section: [{0}]", s.Name))
                oWriter.WriteLine(String.Format("[{0}]", s.Name))
                For Each k As IniSection.IniKey In s.Keys
                    If k.Value <> String.Empty Then
                        Trace.WriteLine(String.Format("Writing Key: {0}={1}", k.Name, k.Value))
                        oWriter.WriteLine(String.Format("{0}={1}", k.Name, k.Value))
                    Else
                        Trace.WriteLine(String.Format("Writing Key: {0}", k.Name))
                        oWriter.WriteLine(String.Format("{0}", k.Name))
                    End If
                Next
            Next
            oWriter.Close()
        End Sub

        ' Gets all the sections
        Public ReadOnly Property Sections() As System.Collections.ICollection
            Get
                Return m_sections.Values
            End Get
        End Property

        ' Adds a section to the IniFile object, returns a IniSection object to the new or existing object
        Public Function AddSection(ByVal sSection As String) As IniSection
            Dim s As IniSection = Nothing
            sSection = sSection.Trim()
            ' Trim spaces
            If m_sections.ContainsKey(sSection) Then
                s = DirectCast(m_sections(sSection), IniSection)
            Else
                s = New IniSection(Me, sSection)
                m_sections(sSection) = s
            End If
            Return s
        End Function

        ' Removes a section by its name sSection, returns trus on success
        Public Function RemoveSection(ByVal sSection As String) As Boolean
            sSection = sSection.Trim()
            Return RemoveSection(GetSection(sSection))
        End Function

        ' Removes section by object, returns trus on success
        Public Function RemoveSection(ByVal Section As IniSection) As Boolean
            If Section IsNot Nothing Then
                Try
                    m_sections.Remove(Section.Name)
                    Return True
                Catch ex As Exception
                    Trace.WriteLine(ex.Message)
                End Try
            End If
            Return False
        End Function

        '  Removes all existing sections, returns trus on success
        Public Function RemoveAllSections() As Boolean
            m_sections.Clear()
            Return (m_sections.Count = 0)
        End Function

        ' Returns an IniSection to the section by name, NULL if it was not found
        Public Function GetSection(ByVal sSection As String) As IniSection
            sSection = sSection.Trim()
            ' Trim spaces
            If m_sections.ContainsKey(sSection) Then
                Return DirectCast(m_sections(sSection), IniSection)
            End If
            Return Nothing
        End Function

        '  Returns a KeyValue in a certain section
        Public Function GetKeyValue(ByVal sSection As String, ByVal sKey As String) As String
            Dim s As IniSection = GetSection(sSection)
            If s IsNot Nothing Then
                Dim k As IniSection.IniKey = s.GetKey(sKey)
                If k IsNot Nothing Then
                    Return k.Value
                End If
            End If
            Return String.Empty
        End Function

        ' Sets a KeyValuePair in a certain section
        Public Function SetKeyValue(ByVal sSection As String, ByVal sKey As String, ByVal sValue As String) As Boolean
            Dim s As IniSection = AddSection(sSection)
            If s IsNot Nothing Then
                Dim k As IniSection.IniKey = s.AddKey(sKey)
                If k IsNot Nothing Then
                    k.Value = sValue
                    Return True
                End If
            End If
            Return False
        End Function

        ' Renames an existing section returns true on success, false if the section didn't exist or there was another section with the same sNewSection
        Public Function RenameSection(ByVal sSection As String, ByVal sNewSection As String) As Boolean
            '  Note string trims are done in lower calls.
            Dim bRval As Boolean = False
            Dim s As IniSection = GetSection(sSection)
            If s IsNot Nothing Then
                bRval = s.SetName(sNewSection)
            End If
            Return bRval
        End Function

        ' Renames an existing key returns true on success, false if the key didn't exist or there was another section with the same sNewKey
        Public Function RenameKey(ByVal sSection As String, ByVal sKey As String, ByVal sNewKey As String) As Boolean
            '  Note string trims are done in lower calls.
            Dim s As IniSection = GetSection(sSection)
            If s IsNot Nothing Then
                Dim k As IniSection.IniKey = s.GetKey(sKey)
                If k IsNot Nothing Then
                    Return k.SetName(sNewKey)
                End If
            End If
            Return False
        End Function

        ' Remove a key by section name and key name
        Public Function RemoveKey(ByVal sSection As String, ByVal sKey As String) As Boolean
            Dim s As IniSection = GetSection(sSection)
            If s IsNot Nothing Then
                Return s.RemoveKey(sKey)
            End If
            Return False
        End Function

        ' IniSection class 
        Public Class IniSection
            '  IniFile IniFile object instance
            Private m_pIniFile As IniFile
            '  Name of the section
            Private m_sSection As String
            '  List of IniKeys in the section
            Private m_keys As Hashtable

            ' Constuctor so objects are internally managed
            Protected Friend Sub New(ByVal parent As IniFile, ByVal sSection As String)
                m_pIniFile = parent
                m_sSection = sSection
                m_keys = New Hashtable(StringComparer.InvariantCultureIgnoreCase)
            End Sub

            ' Returns all the keys in a section
            Public ReadOnly Property Keys() As System.Collections.ICollection
                Get
                    Return m_keys.Values
                End Get
            End Property

            ' Returns the section name
            Public ReadOnly Property Name() As String
                Get
                    Return m_sSection
                End Get
            End Property

            ' Adds a key to the IniSection object, returns a IniKey object to the new or existing object
            Public Function AddKey(ByVal sKey As String) As IniKey
                sKey = sKey.Trim()
                Dim k As IniSection.IniKey = Nothing
                If sKey.Length <> 0 Then
                    If m_keys.ContainsKey(sKey) Then
                        k = DirectCast(m_keys(sKey), IniKey)
                    Else
                        k = New IniSection.IniKey(Me, sKey)
                        m_keys(sKey) = k
                    End If
                End If
                Return k
            End Function

            ' Removes a single key by string
            Public Function RemoveKey(ByVal sKey As String) As Boolean
                Return RemoveKey(GetKey(sKey))
            End Function

            ' Removes a single key by IniKey object
            Public Function RemoveKey(ByVal Key As IniKey) As Boolean
                If Key IsNot Nothing Then
                    Try
                        m_keys.Remove(Key.Name)
                        Return True
                    Catch ex As Exception
                        Trace.WriteLine(ex.Message)
                    End Try
                End If
                Return False
            End Function

            ' Removes all the keys in the section
            Public Function RemoveAllKeys() As Boolean
                m_keys.Clear()
                Return (m_keys.Count = 0)
            End Function

            ' Returns a IniKey object to the key by name, NULL if it was not found
            Public Function GetKey(ByVal sKey As String) As IniKey
                sKey = sKey.Trim()
                If m_keys.ContainsKey(sKey) Then
                    Return DirectCast(m_keys(sKey), IniKey)
                End If
                Return Nothing
            End Function

            ' Sets the section name, returns true on success, fails if the section
            ' name sSection already exists
            Public Function SetName(ByVal sSection As String) As Boolean
                sSection = sSection.Trim()
                If sSection.Length <> 0 Then
                    ' Get existing section if it even exists...
                    Dim s As IniSection = m_pIniFile.GetSection(sSection)
                    If s IsNot Me AndAlso s IsNot Nothing Then
                        Return False
                    End If
                    Try
                        ' Remove the current section
                        m_pIniFile.m_sections.Remove(m_sSection)
                        ' Set the new section name to this object
                        m_pIniFile.m_sections(sSection) = Me
                        ' Set the new section name
                        m_sSection = sSection
                        Return True
                    Catch ex As Exception
                        Trace.WriteLine(ex.Message)
                    End Try
                End If
                Return False
            End Function

            ' Returns the section name
            Public Function GetName() As String
                Return m_sSection
            End Function

            ' IniKey class
            Public Class IniKey
                '  Name of the Key
                Private m_sKey As String
                '  Value associated
                Private m_sValue As String
                '  Pointer to the parent CIniSection
                Private m_section As IniSection

                ' Constuctor so objects are internally managed
                Protected Friend Sub New(ByVal parent As IniSection, ByVal sKey As String)
                    m_section = parent
                    m_sKey = sKey
                End Sub

                ' Returns the name of the Key
                Public ReadOnly Property Name() As String
                    Get
                        Return m_sKey
                    End Get
                End Property

                ' Sets or Gets the value of the key
                Public Property Value() As String
                    Get
                        Return m_sValue
                    End Get
                    Set(ByVal value As String)
                        m_sValue = value
                    End Set
                End Property

                ' Sets the value of the key
                Public Sub SetValue(ByVal sValue As String)
                    m_sValue = sValue
                End Sub
                ' Returns the value of the Key
                Public Function GetValue() As String
                    Return m_sValue
                End Function

                ' Sets the key name
                ' Returns true on success, fails if the section name sKey already exists
                Public Function SetName(ByVal sKey As String) As Boolean
                    sKey = sKey.Trim()
                    If sKey.Length <> 0 Then
                        Dim k As IniKey = m_section.GetKey(sKey)
                        If k IsNot Me AndAlso k IsNot Nothing Then
                            Return False
                        End If
                        Try
                            ' Remove the current key
                            m_section.m_keys.Remove(m_sKey)
                            ' Set the new key name to this object
                            m_section.m_keys(sKey) = Me
                            ' Set the new key name
                            m_sKey = sKey
                            Return True
                        Catch ex As Exception
                            Trace.WriteLine(ex.Message)
                        End Try
                    End If
                    Return False
                End Function

                ' Returns the name of the Key
                Public Function GetName() As String
                    Return m_sKey
                End Function
            End Class
            ' End of IniKey class
        End Class
        ' End of IniSection class

        Protected Overrides Sub Finalize()
            MyBase.Finalize()
        End Sub
    End Class
    ' End of IniFile class
End Module
