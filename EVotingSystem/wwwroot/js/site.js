(() => {
  const dashboard = document.querySelector("[data-results-dashboard]");
  if (!dashboard) {
    return;
  }

  const refreshUrl = dashboard.getAttribute("data-refresh-url");
  const refreshSeconds = Number.parseInt(dashboard.getAttribute("data-refresh-seconds") || "15", 10);
  if (!refreshUrl || refreshSeconds <= 0) {
    return;
  }

  const totalVotesElement = document.querySelector("[data-results-total-votes]");
  const turnoutElement = document.querySelector("[data-results-turnout]");
  const generatedElement = document.querySelector("[data-results-generated]");
  const statusElement = document.querySelector("[data-results-status]");
  const zeroVoteElement = document.querySelector("[data-results-zero-state]");
  const emptyStateElement = document.querySelector("[data-results-empty-state]");
  const candidateCards = () => Array.from(document.querySelectorAll("[data-candidate-id]"));

  const updateDashboard = async () => {
    try {
      const response = await fetch(refreshUrl, {
        headers: {
          "X-Requested-With": "XMLHttpRequest"
        },
        cache: "no-store"
      });

      if (!response.ok) {
        return;
      }

      const payload = await response.json();
      if (candidateCards().length !== payload.candidateResults.length) {
        window.location.reload();
        return;
      }

      if (totalVotesElement) {
        totalVotesElement.textContent = String(payload.totalVotesCast);
      }

      if (turnoutElement) {
        turnoutElement.textContent = `${payload.turnoutPercentage}%`;
      }

      if (generatedElement) {
        generatedElement.textContent = new Date(payload.generatedAtUtc).toLocaleString();
      }

      if (statusElement) {
        statusElement.textContent = payload.electionOpen ? "Open" : "Closed";
      }

      if (zeroVoteElement) {
        zeroVoteElement.classList.toggle("d-none", !payload.zeroVoteState);
      }

      if (emptyStateElement) {
        emptyStateElement.classList.toggle("d-none", !payload.noCandidatesState);
      }

      for (const candidate of payload.candidateResults) {
        const voteElement = document.querySelector(`[data-candidate-votes="${candidate.candidateId}"]`);
        const shareElement = document.querySelector(`[data-candidate-share="${candidate.candidateId}"]`);
        const progressElement = document.querySelector(`[data-candidate-progress="${candidate.candidateId}"]`);
        const cardElement = document.querySelector(`[data-candidate-id="${candidate.candidateId}"] .results-card`);
        const leadingElement = document.querySelector(`[data-candidate-leading="${candidate.candidateId}"]`);

        if (voteElement) {
          voteElement.textContent = String(candidate.voteCount);
        }

        if (shareElement) {
          shareElement.textContent = `${candidate.votePercentage}%`;
        }

        if (progressElement) {
          progressElement.style.width = `${candidate.votePercentage}%`;
          progressElement.setAttribute("aria-valuenow", String(candidate.votePercentage));
        }

        if (cardElement) {
          cardElement.classList.toggle("results-card-leading", Boolean(candidate.isLeading));
        }

        if (leadingElement) {
          leadingElement.classList.toggle("d-none", !candidate.isLeading);
        }
      }
    } catch {
      // Keep the last rendered state if refresh fails.
    }
  };

  window.setInterval(updateDashboard, refreshSeconds * 1000);
})();
