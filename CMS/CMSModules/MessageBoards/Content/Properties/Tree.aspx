<%@ Page Language="C#" AutoEventWireup="true" Inherits="CMSModules_MessageBoards_Content_Properties_Tree"
    Theme="Default" MasterPageFile="~/CMSMasterPages/UI/Tree.master" Title="Message boards - tree"
     Codebehind="Tree.aspx.cs" %>

<asp:Content ID="cntMenu" runat="server" ContentPlaceHolderID="plcMenu">
    <div class="tree-actions-panel">
        <div class="tree-actions">
            <cms:CMSAccessibleButton runat="server" ID="btnDelete" IconCssClass="icon-bin" IconOnly="true" />
        </div>
    </div>
</asp:Content>
<asp:Content ID="cntBody" runat="server" ContentPlaceHolderID="plcTree">
    <script type="text/javascript">
        //<![CDATA[

        // Refresh node action
        function RefreshNode(nodeName, nodeId) {
            currentNode.firstChild.innerHTML = nodeName;
            // Dynamically create onclick event
            currentNode.onclick = function () { SelectNode(nodeName, this); };
        }

        //]]>
    </script>
    <asp:HiddenField ID="hdnBoardId" runat="server" />
    <asp:TreeView ID="treeElem" runat="server" ShowLines="True" CssClass="ContentTree" EnableViewState="False" />
    <asp:Literal ID="ltlScript" runat="server" EnableViewState="false" />
</asp:Content>
