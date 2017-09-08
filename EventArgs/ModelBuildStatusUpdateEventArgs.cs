namespace STAADModel
{
    public class ModelBuildStatusUpdateEventArgs
    {
        public string StatusMessage { get; set; }

        public int ElementsProcessed { get; set; }

        public int TotalElementsToProcess { get; set; }

        public double CompletionRate
        {
            get { return (double)this.ElementsProcessed / this.TotalElementsToProcess; }
        }

        public ModelBuildStatusUpdateEventArgs(string StatusMessage, int ElementsProcessed, int TotalElementsToProcess)
        {
            this.StatusMessage = StatusMessage;
            this.ElementsProcessed = ElementsProcessed;
            this.TotalElementsToProcess = TotalElementsToProcess;
        }
    }
}