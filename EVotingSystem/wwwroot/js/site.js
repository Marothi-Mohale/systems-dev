document.querySelectorAll(".ballot-card").forEach((card) => {
  card.addEventListener("click", () => {
    const input = card.querySelector("input[type='radio']");
    if (input) {
      input.checked = true;
    }
  });
});
