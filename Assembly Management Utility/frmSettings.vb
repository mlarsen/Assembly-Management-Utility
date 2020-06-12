Public Class frmSettings
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()

    End Sub

    Private Sub txtAssemblyFilter_Leave(sender As Object, e As EventArgs) Handles txtAssemblyFilter.Leave
        stAssemblyFilter = Me.txtAssemblyFilter.Text

        CreateUpdateIniFile("AssemblyFilter", stAssemblyFilter)
    End Sub

    Private Sub txtKitFilter_Leave(sender As Object, e As EventArgs) Handles txtKitFilter.Leave
        stKitSuffix = Me.txtKitFilter.Text
        CreateUpdateIniFile("KitSuffix", stKitSuffix)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        LoadForm()
        Me.Close()
    End Sub

    Private Sub frmSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        'Add the public variables to the fields
        Me.txtAssemblyFilter.Text = stAssemblyFilter
        Me.txtKitFilter.Text = stKitSuffix
        CenterToParent()
    End Sub
End Class