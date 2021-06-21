window.midway = {
  scrollLog: function (id) {
    var textArea;

    textArea = document.getElementById(id);
    textArea.scrollTop = textArea.scrollHeight;
  },
  focus: function (id) {
    var ctrl;

    ctrl = document.getElementById(id);
    ctrl.focus();
  }
};