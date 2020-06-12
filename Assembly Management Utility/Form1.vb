Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim stSQL As String

        'Make sure the path is valid
        If Strings.Right(stPath, 1) <> "\" Then
            stPath = stPath + "\"
        End If

        Me.txtPath.Text = stPath

        'Get the version infomration
        Me.txtVersion.Text = "Version:" + Reflection.Assembly.GetExecutingAssembly.GetName.Version.Major.ToString + "." + Reflection.Assembly.GetExecutingAssembly.GetName.Version.Minor.ToString + "." + Reflection.Assembly.GetExecutingAssembly.GetName.Version.Build.ToString + "." + Reflection.Assembly.GetExecutingAssembly.GetName.Version.Revision.ToString

        'Get the DSN
        stODBCString = GetDSN(stPath)
        If stODBCString <> "" Then
            If bolIsClientServer = True Then


                If IsLicensed("Assembly/Kit Tool") = False Then
                    MessageBox.Show("This tool is not licensed.")
                    Close()
                Else

                    CreateTables()

                    'Get the Assembly filter and the kit suffix
                    If My.Computer.FileSystem.FileExists(stPath + "AssemblyManagement.ini") = True Then
                        If GetSettings(stPath) = "" Then
                            frmSettings.Show()
                        Else
                            LoadForm()
                        End If

                    Else
                        frmSettings.Show()
                    End If

                End If
            Else
                MessageBox.Show("This tool will only work with a Client Server installation")
                Close()
            End If
        End If

        CenterToParent()
    End Sub

    Private Sub cmboSelectAssembly_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmboSelectAssembly.SelectedIndexChanged
        Dim stSQL As String
        stAssemblyProd = Me.cmboSelectAssembly.SelectedItem
        stAssemblyKit = stAssemblyProd + "-KIT"

        stSQL = "SELECT Description,InStock FROM Products WHERE ProductNo='" + stAssemblyProd + "';"
        SQLTextQuery("S", stSQL, stODBCString, 2)
        Me.txtDescription.Text = sqlArray(0)
        Me.txtInStock.Text = sqlArray(1)
        Me.txtQuantity.Text = "0"
        Me.txtNewInstock.Clear()

        stSQL = "SELECT A.AddProductNo AS ProductNo,P.Description,A.Quantity AS AssemblyQty,P.InStock,0 AS NewInStock ,0.00 AS UnitCost FROM Assembly A JOIN Products P ON A.AddProductNo=P.ProductNo WHERE A.ProductNo='" + stAssemblyKit + "';"
        LoadDataGridViewSQL(stSQL, Me.DataGridView1)
    End Sub


    Private Sub txtQuantity_Leave(sender As Object, e As EventArgs) Handles txtQuantity.Leave
        iAdjustQty = CInt(txtQuantity.Text)
        Dim iQuantity As Integer
        Dim iInStock As Integer
        Dim iNewInStock As Integer

        bolIsError = False

        Me.txtNewInstock.Text = CStr(CInt(Me.txtInStock.Text) + iAdjustQty)

        If CInt(Me.txtNewInstock.Text) < 0 Then
            Me.txtNewInstock.BackColor = Color.Red
            bolIsError = True
        Else
            Me.txtNewInstock.BackColor = Color.White
        End If

        For i = 0 To Me.DataGridView1.Rows.Count - 1
            iQuantity = Me.DataGridView1.Rows(i).Cells("AssemblyQty").Value
            iInStock = Me.DataGridView1.Rows(i).Cells("InStock").Value
            iNewInStock = iInStock - iQuantity * iAdjustQty
            Me.DataGridView1.Rows(i).Cells("NewInStock").Value = iNewInStock
            If iNewInStock < 0 Then
                Me.DataGridView1.Rows(i).DefaultCellStyle.BackColor = Color.Red
                bolIsError = True
            Else
                Me.DataGridView1.Rows(i).DefaultCellStyle.BackColor = Color.White
            End If
        Next
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If bolIsError = True Then
            MessageBox.Show("You cannot make the adjustment requested.")
        Else
            AdjustKit()
            If bolIsError = True Then
                MessageBox.Show("The update could not be completed")
            Else
                MessageBox.Show("Product " + stAssemblyProd + " and related components were updated.")
            End If
        End If

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        frmSettings.Show()
    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        System.Diagnostics.Process.Start("https://www.channergy.com/features/tools/assembly-management-tool/")
    End Sub
End Class
