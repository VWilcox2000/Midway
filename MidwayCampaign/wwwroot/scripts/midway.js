window.midway = {
  scrollLog: function (id) {
    var textArea;

    textArea = document.getElementById(id);
    textArea.scrollTop = textArea.scrollHeight;
  }
};